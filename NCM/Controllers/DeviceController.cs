using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Renci.SshNet; // Thêm thư viện SSH.NET
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCM.Data;
using NCM.Models;
using Renci.SshNet.Common;

namespace NCM.Controllers
{
    public class DeviceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DeviceController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ===== Device CRUD =====

        public async Task<IActionResult> Index()
        {
            var list = await _context.Devices.AsNoTracking().ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var device = await _context.Devices
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == id);
            if (device == null) return NotFound();
            return View(device);
        }

     


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DeviceId,Name,IPAddress,Type,Status")] Device device)
        {
            if (id != device.DeviceId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(device);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Devices.AnyAsync(e => e.DeviceId == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var device = await _context.Devices
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ===== Backup Configuration =====

        // GET: /Device/Backup/5
        public async Task<IActionResult> Backup(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();
            return View(device);
        }

        // POST: /Device/BackupConfirmed/5
        [HttpPost, ActionName("BackupConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupConfirmed(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                TempData["Message"] = "Thiết bị không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            // Ensure SSH credentials exist
            if (string.IsNullOrEmpty(device.SshUsername) || string.IsNullOrEmpty(device.SshPassword))
            {
                TempData["Message"] = "Thiết bị chưa có thông tin SSH!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Step 1: Connect via SSH and fetch the running configuration
                string configText;
                using (var ssh = new SshClient(device.IPAddress, 22, device.SshUsername, device.SshPassword))
                {
                    ssh.Connect();
                    configText = ssh.RunCommand("show running-config").Result;
                    ssh.Disconnect();
                }

                // Step 2: Save the configuration to the database
                var newConfig = new DeviceConfig
                {
                    DeviceId = id,
                    ConfigText = configText,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DeviceConfigs.Add(newConfig);

                // Step 3: Update the device's last backup time
                device.LastBackupTime = newConfig.CreatedAt;
                _context.Devices.Update(device);

                await _context.SaveChangesAsync();

                TempData["Message"] = "Backup thành công và cấu hình đã được lưu.";
            }
            catch (SshException sshEx)
            {
                // Handle SSH-specific errors
                TempData["Message"] = $"Lỗi kết nối SSH: {sshEx.Message}";
            }
            catch (Exception ex)
            {
                // Handle general errors
                TempData["Message"] = $"Backup lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== 1) Xem lịch sử backup =====

        // GET: /Device/Configs/5
        public async Task<IActionResult> Configs(int id)
        {
            var device = await _context.Devices
                .Include(d => d.DeviceConfigs)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == id);
            if (device == null) return NotFound();

            ViewBag.Device = device;
            var configs = device.DeviceConfigs
                                .OrderByDescending(c => c.CreatedAt)
                                .ToList();
            return View(configs);
        }

        // ===== 2) So sánh diff =====

        // GET: /Device/Diff?oldId=3&newId=4
        public async Task<IActionResult> Diff(int oldId, int newId)
        {
            var oldCfg = await _context.DeviceConfigs.FindAsync(oldId);
            var newCfg = await _context.DeviceConfigs.FindAsync(newId);
            if (oldCfg == null || newCfg == null) return NotFound();

            var differ = new Differ();
            var builder = new SideBySideDiffBuilder(differ);
            var diff = builder.BuildDiffModel(oldCfg.ConfigText, newCfg.ConfigText);

            ViewBag.OldConfig = oldCfg;
            ViewBag.NewConfig = newCfg;
            return View(diff);
        }

        // ===== 3) Compliance =====

        // GET: /Device/ComplianceRules
        public async Task<IActionResult> ComplianceRules()
        {
            var rules = await _context.ComplianceRules
                .AsNoTracking()
                .ToListAsync();
            return View(rules);
        }

        // GET: /Device/CheckCompliance/5
        public async Task<IActionResult> CheckCompliance(int configId)
        {
            var cfg = await _context.DeviceConfigs.FindAsync(configId);
            if (cfg == null) return NotFound();

            // Xoá kết quả cũ
            var oldR = _context.ComplianceResults
                .Where(r => r.ConfigId == configId);
            _context.ComplianceResults.RemoveRange(oldR);

            var rules = await _context.ComplianceRules.ToListAsync();
            foreach (var r in rules)
            {
                bool ok = true;
                if (!string.IsNullOrEmpty(r.MustContain)
                    && !cfg.ConfigText.Contains(r.MustContain))
                    ok = false;
                if (!string.IsNullOrEmpty(r.MustNotContain)
                    && cfg.ConfigText.Contains(r.MustNotContain))
                    ok = false;

                _context.ComplianceResults.Add(new ComplianceResult
                {
                    ConfigId = configId,
                    RuleId = r.RuleId,
                    IsCompliant = ok,
                    CheckedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ComplianceResults), new { configId });
        }

        // GET: /Device/ComplianceResults/5
        public async Task<IActionResult> ComplianceResults(int configId)
        {
            var results = await _context.ComplianceResults
                .Include(r => r.ComplianceRule)
                .Where(r => r.ConfigId == configId)
                .OrderByDescending(r => r.CheckedAt)
                .ToListAsync();
            ViewBag.ConfigId = configId;
            return View(results);
        }


        // GET: /Device/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Device/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,IPAddress,Type,Status")] Device device,
            string SshUsername,
            string SshPassword)
        {
            if (ModelState.IsValid)
            {
                // Validate SSH credentials before saving
                try
                {
                    using var client = new SshClient(device.IPAddress, 22, SshUsername, SshPassword);
                    client.Connect();

                    // Test a basic command to ensure SSH works
                    var cmd = client.RunCommand("show running-config");
                    var result = cmd.Result;

                    client.Disconnect();

                    // Save the device to the database after successful SSH validation
                    _context.Add(device);
                    await _context.SaveChangesAsync();

                    // Save SSH credentials securely
                    string sshInfo = $"{device.IPAddress},{SshUsername},{SshPassword}";
                    string sshDir = Path.Combine(_env.ContentRootPath, "SSH");
                    Directory.CreateDirectory(sshDir);
                    System.IO.File.WriteAllText(Path.Combine(sshDir, $"{device.DeviceId}.ssh"), sshInfo);

                    TempData["Message"] = "Device added successfully, and SSH is valid!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Handle SSH errors
                    ModelState.AddModelError(string.Empty, $"SSH validation failed: {ex.Message}");
                }
            }
            return View(device);
        }

    }
}
