
#!/usr/bin/env python3
import os
import sys
import datetime
from netmiko import ConnectHandler
import requests

# ====== Cấu hình router ======
# Hoặc load từ JSON/CSV để backup nhiều device
router = {
    'device_type': 'cisco_ios',
    'host':   'sandbox-iosxr-1.cisco.com',
    'username': 'admin',
    'password': 'C1sco12345',
      # nếu cần enable mode
    'port':     22,
    # 'verbose':  True,
}

# ====== Thư mục lưu backup ======
BACKUP_DIR = os.path.join(os.path.dirname(__file__), 'backups')
os.makedirs(BACKUP_DIR, exist_ok=True)

def backup_router_cfg(dev):
    """Kết nối SSH, lấy running-config, lưu file và (tùy) POST lên API."""
    print(f"[+] Connecting to {dev['host']} …")
    try:
        conn = ConnectHandler(**dev)
        # nếu cần vào enable mode
        if 'secret' in dev and dev['secret']:
            conn.enable()
        
        print("[+] Fetching running-config …")
        output = conn.send_command('show running-config')
        conn.disconnect()
    except Exception as e:
        print(f"[!] SSH error: {e}")
        sys.exit(1)
    
    # Tạo file tên như: 192.168.100.2_20250423-153045.txt
    now = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    fname = f"{dev['host']}_{now}.txt"
    path = os.path.join(BACKUP_DIR, fname)
    
    with open(path, 'w') as f:
        f.write(output)
    print(f"[+] Saved backup to {path}")
    
    # --- Tùy chọn: đẩy lên Web API ---
    API_URL = "http://localhost:7148/api/deviceconfigs"  # đổi theo web‑app của bạn
    payload = {
        "deviceIp": dev['host'],
        "timestamp": now,
        "configText": output
    }
    try:
        resp = requests.post(API_URL, json=payload, timeout=10)
        if resp.status_code == 201:
            print("[+] Posted config to web‑app API")
        else:
            print(f"[!] API returned {resp.status_code}: {resp.text}")
    except Exception as e:
        print(f"[!] API error: {e}")

if __name__ == "__main__":
    backup_router_cfg(router)
