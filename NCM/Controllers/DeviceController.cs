using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;
using DiffPlex.DiffBuilder;
using NCM.Data;
using NCM.Models;
using DiffPlex;
using NCM.Services;
using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NCM.Controllers
{
    public class DeviceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;
        private readonly SnmpService _snmpService;


        public DeviceController(
            ApplicationDbContext context,
            IDataProtectionProvider dataProtectionProvider,
                SnmpService snmpService)      // ← thêm vào

        {
            _context = context;
            _protector = dataProtectionProvider.CreateProtector("SSHPasswords");
            _snmpService = snmpService;
        }

        // GET: Device
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách devices từ database
            var devices = await _context.Devices.ToListAsync();

            // Tạo một list các tác vụ ping
            var pingTasks = devices.Select(async device =>
            {
                try
                {
                    using var ping = new Ping(); // Ping class để kiểm tra host :contentReference[oaicite:0]{index=0}
                    var reply = await ping.SendPingAsync(device.IPAddress, 1000);
                    device.Status = (reply.Status == IPStatus.Success); // true nếu nhận ICMP reply :contentReference[oaicite:1]{index=1}
                }
                catch
                {
                    device.Status = false; // coi là offline nếu có exception :contentReference[oaicite:2]{index=2}
                }
                return device;
            });

            // Chờ tất cả ping hoàn thành
            var model = await Task.WhenAll(pingTasks);

            return View(model);
        }

        // GET: Device/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Device/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,IPAddress,Type,Status,SshUsername,SshPassword")] Device device)
        {
            if (!ModelState.IsValid)
                return View(device);

            // 1. Ping test
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync(device.IPAddress, 2000);
                if (reply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    ModelState.AddModelError(
                        nameof(device.IPAddress),
                        "Không thể ping đến IP này. Vui lòng kiểm tra kết nối.");
                    return View(device);
                }
            }
            catch
            {
                ModelState.AddModelError(
                    nameof(device.IPAddress),
                    "Lỗi ping. IP không hợp lệ hoặc mạng có vấn đề.");
                return View(device);
            }

            // 2. SSH credential test
            var rawPassword = device.SshPassword;
            try
            {
                using var ssh = new SshClient(
                    device.IPAddress,
                    device.SshUsername,
                    rawPassword)
                {
                    ConnectionInfo = { Timeout = TimeSpan.FromSeconds(5) }
                };
                ssh.Connect();
                if (!ssh.IsConnected)
                    throw new Exception("SSH không kết nối được.");
                ssh.Disconnect();
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "SSH login thất bại. Kiểm tra lại username/password và SSH port.");
                return View(device);
            }

            // 3. Protect password & save to DB
            device.SshPassword = _protector.Protect(rawPassword);
            _context.Add(device);
            await _context.SaveChangesAsync();

            // 4. Write SSH script file
            var sshFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "SSH");
            Directory.CreateDirectory(sshFolder);
            var scriptPath = Path.Combine(
                sshFolder, $"{device.DeviceId}.ssh");
            await System.IO.File.WriteAllTextAsync(
                scriptPath,
                $"ssh {device.SshUsername}@{device.IPAddress}\n");

            return RedirectToAction(nameof(Index));

        }


        // GET: Device/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            // Unprotect to show in form
            device.SshPassword = _protector.Unprotect(device.SshPassword);
            return View(device);
        }

        // POST: Device/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("DeviceId,Name,IPAddress,Type,Status,SshUsername,SshPassword,LastBackupTime")] Device device)
        {
            if (id != device.DeviceId) return NotFound();
            if (!ModelState.IsValid) return View(device);

            // Optionally repeat ping/SSH test here...

            // Protect password before save
            device.SshPassword = _protector.Protect(device.SshPassword);

            try
            {
                _context.Update(device);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Devices.Any(e => e.DeviceId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Device/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var device = await _context.Devices
                .FirstOrDefaultAsync(m => m.DeviceId == id);
            if (device == null) return NotFound();

            return View(device);
        }

        // POST: Device/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
                // Remove SSH script
                var sshFile = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "SSH", $"{id}.ssh");
                if (System.IO.File.Exists(sshFile))
                    System.IO.File.Delete(sshFile);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Device/Configs/5
        public async Task<IActionResult> Configs(int? id)
        {
            if (id == null) return NotFound();

            var device = await _context.Devices
                .Include(d => d.DeviceConfigs)
                .FirstOrDefaultAsync(d => d.DeviceId == id);
            if (device == null) return NotFound();

            ViewBag.Device = device;
            return View(device.DeviceConfigs);
        }

        // GET: Device/Diff?id=...&oldId=...
        public async Task<IActionResult> Diff(int id, int oldId)
        {
            var newCfg = await _context.DeviceConfigs
                .FirstOrDefaultAsync(c => c.ConfigId == id);
            var oldCfg = await _context.DeviceConfigs
                .FirstOrDefaultAsync(c => c.ConfigId == oldId);
            if (newCfg == null || oldCfg == null)
                return NotFound();

            var differ = new SideBySideDiffBuilder(new Differ());
            var model = differ.BuildDiffModel(
                oldCfg.ConfigContent,
                newCfg.ConfigContent);

            return View(model);
        }

        // POST or GET: Device/CheckCompliance/5
        public async Task<IActionResult> CheckCompliance(int id)
        {
            var config = await _context.DeviceConfigs.FindAsync(id);
            if (config == null) return NotFound();

            var rules = await _context.ComplianceRules.ToListAsync();

            // Remove old results
            var oldResults = _context.ComplianceResults
                .Where(r => r.ConfigId == id);
            _context.ComplianceResults.RemoveRange(oldResults);

            // Evaluate each rule
            foreach (var rule in rules)
            {
                bool ok = config.ConfigContent
                    .Contains(rule.RequiredString);
                var result = new ComplianceResult
                {
                    ConfigId = id,
                    RuleId = rule.RuleId,
                    IsCompliant = ok,
                    CheckedAt = DateTime.UtcNow
                };
                _context.ComplianceResults.Add(result);
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ComplianceResults), new { id });
        }

        // GET: Device/ComplianceResults/5
        public async Task<IActionResult> ComplianceResults(int id)
        {
            var results = await _context.ComplianceResults
                .Include(r => r.ComplianceRule)
                .Where(r => r.ConfigId == id)
                .ToListAsync();
            if (results.Count == 0)
                ViewBag.Message = "Chưa có kết quả compliance. Vui lòng kiểm tra.";
            return View(results);
        }
        public IActionResult SnmpInfo(int id)
        {
            var device = _context.Devices.Find(id);
            if (device == null) return NotFound();

            var info = _snmpService.GetBasicInfo(device.IPAddress);
            ViewBag.Device = device;
            return View(info);
        }
        // GET: Device/Dashboard/5
        public async Task<IActionResult> Dashboard(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            // Lấy latest metrics
            var latest = await _context.DeviceSnmpMetrics
                .Where(m => m.DeviceId == id)
                .GroupBy(m => m.MetricName)
                .Select(g => g.OrderByDescending(x => x.Timestamp).FirstOrDefault())
                .ToListAsync();

            // Lấy lịch sử uptime 24h
            var history = await _context.DeviceSnmpMetrics
                .Where(m => m.DeviceId == id && m.MetricName == "SysUpTime" &&
                            m.Timestamp >= DateTime.UtcNow.AddHours(-24))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var vm = new Dashboard
            {
                Device = device,
                SysName = latest.FirstOrDefault(m => m.MetricName == "SysName")?.MetricValue,
                SysDescr = latest.FirstOrDefault(m => m.MetricName == "SysDescr")?.MetricValue,
                Uptime = latest.FirstOrDefault(m => m.MetricName == "SysUpTime")?.MetricValue,
                History = history
            };
            return View(vm);
        }
        public async Task<IActionResult> ConfigDiffs(int deviceId)
        {
            // Lấy tất cả diff của thiết bị, sắp xếp theo thời gian giảm dần
            var diffs = await _context.DeviceConfigDiffs
                .Where(d => d.DeviceId == deviceId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(diffs);
        }

    }
}
