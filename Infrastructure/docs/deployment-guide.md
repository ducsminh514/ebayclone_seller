# Hướng Dẫn Triển Khai Từ Đầu Đến Cuối
## Redis Docker + Nginx + Multi-Instance API

> Hướng dẫn này dành cho Windows. Đi từng bước, không bỏ bước nào.

---

## Bước 1: Cài Docker Desktop

> Nếu đã có Docker Desktop thì bỏ qua bước này.

1. Tải Docker Desktop: https://www.docker.com/products/docker-desktop/
2. Cài đặt → khởi động lại máy
3. Mở Docker Desktop → đợi Docker Engine sáng xanh
4. Verify:
```powershell
docker --version
# → Docker version 24.x.x hoặc mới hơn
```

---

## Bước 2: Cài Redis Bằng Docker

Mở **PowerShell** (Run as Administrator):

```powershell
# Pull image Redis phiên bản nhẹ (Alpine Linux ~5MB)
docker pull redis:7-alpine

# Tạo và start Redis container
docker run -d --name redis -p 6380:6379 --restart unless-stopped redis:7-alpine
```

Giải thích:
- `-d` = chạy background (detached)
- `--name redis` = đặt tên container là "redis"
- `-p 6380:6379` = map port 6380 từ container ra máy host
- `--restart unless-stopped` = tự khởi động lại khi máy reboot

**Verify Redis đang chạy:**
```powershell
docker exec redis redis-cli ping
# → PONG  ← nếu thấy PONG là thành công
```

**Lệnh quản lý Redis cơ bản:**
```powershell
docker stop redis      # Dừng Redis
docker start redis     # Khởi động lại Redis
docker logs redis      # Xem log Redis
docker rm -f redis     # Xóa container (data mất)
```

---

## Bước 3: Chạy SQL Migration (Indexes)

Mở **SQL Server Management Studio (SSMS)** hoặc dùng terminal:

```powershell
# Mở file SQL
notepad "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Infrastructure\Migrations\AddPerformanceIndexes.sql"
```

Copy nội dung file → paste vào SSMS → chọn database `EbaySellerClone` → Execute.

> [!IMPORTANT]
> File SQL chạy 1 lần duy nhất. Nếu chạy lại sẽ không lỗi vì đã có `IF NOT EXISTS`.

---

## Bước 4: Cài Nginx Trên Windows

### 4.1 Download Nginx

1. Vào https://nginx.org/en/download.html
2. Tìm mục **Stable version** → click "nginx/Windows-x.x.x" để tải file zip
3. Giải nén vào `D:\Downloads\nginx-1.28.2\nginx-1.28.2` (tạo thư mục nếu chưa có)

Cấu trúc sau khi giải nén:
```
D:\Downloads\nginx-1.28.2\nginx-1.28.2\
├── conf\          ← config files
│   ├── nginx.conf ← file chính sẽ thay thế
│   └── mime.types
├── html\          ← default pages
├── logs\          ← access/error logs
└── nginx.exe      ← chương trình chính
```

### 4.2 Copy Config Nginx Từ Project

```powershell
# Backup config gốc
Copy-Item "D:\Downloads\nginx-1.28.2\nginx-1.28.2\conf\nginx.conf" "D:\Downloads\nginx-1.28.2\nginx-1.28.2\conf\nginx.conf.backup"

# Copy config từ project
$projectRoot = "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone"
Copy-Item "$projectRoot\Infrastructure\nginx\nginx.conf" "D:\Downloads\nginx-1.28.2\nginx-1.28.2\conf\nginx.conf" -Force
```

### 4.3 Kiểm Tra Config Syntax

```powershell
cd D:\Downloads\nginx-1.28.2\nginx-1.28.2
.\nginx.exe -t
# → nginx: configuration file D:\Downloads\nginx-1.28.2\nginx-1.28.2/conf/nginx.conf syntax is ok
# → nginx: configuration file D:\Downloads\nginx-1.28.2\nginx-1.28.2/conf/nginx.conf test is successful
```

Nếu thấy `test is successful` → config đúng.

### 4.4 Sửa Path Upload (Nếu Cần)

Mở `D:\Downloads\nginx-1.28.2\nginx-1.28.2\conf\nginx.conf`, tìm dòng:
```nginx
alias C:/Users/Administrator/source/repos/Ebay_Seller_Clone/Ebay_Seller_Clone/Backend/EbayClone.API/wwwroot/uploads/;
```
→ Sửa path nếu project của bạn ở thư mục khác.

> **LƯU Ý:** Dùng dấu `/` (forward slash) chứ không phải `\` (backslash) trong nginx.conf!

---

## Bước 5: Build Project .NET

```powershell
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone"
dotnet build Ebay_Seller_Clone.sln -c Release
```

Phải thấy:
```
Build succeeded.
    0 Error(s)
```

---

## Bước 6: Start API Cluster (3 Instances)

### Cách 1: Dùng Script (Khuyến nghị)

```powershell
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Infrastructure\scripts"
.\start-cluster.ps1
```

Script sẽ:
1. Check Redis connection
2. Build project
3. Start 3 instances trên port 5001, 5002, 5003

### Cách 2: Start Thủ Công (Nếu Script Lỗi)

Mở **3 cửa sổ PowerShell** riêng biệt:

**Cửa sổ 1:**
```powershell
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Backend\EbayClone.API"
dotnet run -c Release --urls http://0.0.0.0:5001
```

**Cửa sổ 2:**
```powershell
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Backend\EbayClone.API"
dotnet run -c Release --urls http://0.0.0.0:5002
```

**Cửa sổ 3:**
```powershell
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Backend\EbayClone.API"
dotnet run -c Release --urls http://0.0.0.0:5003
```

**Verify từng instance:**
```powershell
Invoke-RestMethod http://localhost:5001/health
Invoke-RestMethod http://localhost:5002/health
Invoke-RestMethod http://localhost:5003/health
# → Mỗi cái phải trả: status = Healthy
```

---

## Bước 7: Start Nginx

```powershell
cd D:\Downloads\nginx-1.28.2\nginx-1.28.2
start nginx
```

**Verify Nginx đang chạy:**
```powershell
# Kiểm tra process
Get-Process nginx
# → Thấy 2 process nginx (master + worker)

# Gọi qua Nginx
Invoke-RestMethod http://localhost/health
# → Healthy  ← Nginx đã forward đến 1 trong 3 instances
```

> **LƯU Ý:** Chỉ chạy `start nginx` **1 LẦN DUY NHẤT**. Nếu chạy nhiều lần sẽ tạo nhiều process nginx zombie.
> Nếu cần restart: `nginx.exe -s reload` (không cần stop/start lại)

---

## Bước 8: Start Frontend Blazor

Mở thêm **1 cửa sổ PowerShell** mới:

### Chế độ 1: Chạy với Nginx (khuyến nghị)

Frontend gọi API qua Nginx (port 80) — có load balancing + rate limiting:

```powershell
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Frontend\EbayClone.Frontend"
dotnet run --environment Production
```
→ Mở browser: `https://localhost:7251`

### Chế độ 2: Chạy không Nginx (dev đơn giản)

Frontend gọi API trực tiếp port 7250 — cần chạy riêng 1 instance Kestrel API:

```powershell
# Terminal 1: Start API đơn
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Backend\EbayClone.API"
dotnet run

# Terminal 2: Start Frontend
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Frontend\EbayClone.Frontend"
dotnet run
```
→ Mở browser: `https://localhost:7251`

**Config tự động chọn:**
- `appsettings.json` → Development → `https://localhost:7250` (API đơn)
- `appsettings.Production.json` → Production → `http://localhost` (qua Nginx)

---

## Bước 9: Test Load Balancing

```powershell
# Gọi 10 request liên tiếp qua Nginx
for ($i = 1; $i -le 10; $i++) {
    $response = Invoke-RestMethod http://localhost/health
    Write-Host "Request $i : $($response.status)"
}
```

Mở log Nginx để xem phân phối:
```powershell
Get-Content "D:\Downloads\nginx-1.28.2\nginx-1.28.2\logs\access.log" -Tail 10
# → Cột upstream= sẽ luân phiên: 127.0.0.1:5001, 5002, 5003
```

---

## Bước 10: Test Distributed Lock (Redis)

```powershell
# Xem lock keys trong Redis
docker exec redis redis-cli keys "lock:*"
# → Khi background service đang chạy, thấy ví dụ:
#    lock:fund-release
#    lock:compute-analytics

# Xem giá trị lock (ai đang giữ)
docker exec redis redis-cli get "lock:fund-release"
# → "DESKTOP-XXX:12345"  ← tên máy + PID của instance đang giữ lock

# Monitor real-time (xem mọi command Redis nhận được)
docker exec redis redis-cli monitor
# → Ctrl+C để thoát
```

---

## Bước 11: Test Kill Failover

```powershell
# Xem các processes đang chạy
Get-Process dotnet | Format-Table Id, ProcessName, StartTime

# Kill instance trên port 5001 (lấy PID từ cluster-pids.txt)
$pids = Get-Content "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Infrastructure\scripts\cluster-pids.txt"
Stop-Process -Id $pids[0] -Force

# Gọi health check — vẫn phải thành công (Nginx chuyển sang instance khác)
Invoke-RestMethod http://localhost/health
# → Healthy  ← vẫn hoạt động!

# Xem Nginx error log (sẽ ghi nhận instance 5001 fail)
Get-Content "D:\Downloads\nginx-1.28.2\nginx-1.28.2\logs\error.log" -Tail 5
```

---

## Thứ Tự Dừng Tất Cả

Dừng theo thứ tự ngược:

```powershell
# 1. Dừng Frontend (Ctrl+C trong cửa sổ Frontend)

# 2. Dừng Nginx
cd D:\Downloads\nginx-1.28.2\nginx-1.28.2
.\nginx.exe -s quit

# 3. Dừng API instances
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Infrastructure\scripts"
.\stop-cluster.ps1

# 4. Dừng Redis (optional — giữ chạy để data lock persist)
docker stop redis
```

---

## Kill Toàn Bộ Process (Khi Bị Lỗi / Zombie)

> **Khi nào cần?** Nginx trên Windows dễ tạo zombie process (chạy `start nginx` nhiều lần, hoặc reload không sạch). Dấu hiệu: config mới không apply, port 80 bị chiếm, hoặc `Get-Process nginx` hiện hàng chục process.

### Kill tất cả Nginx

```powershell
# Xem có bao nhiêu nginx đang chạy
Get-Process nginx -ErrorAction SilentlyContinue | Measure-Object | Select-Object -ExpandProperty Count

# Kill tất cả
taskkill /IM nginx.exe /F

# Verify đã sạch (phải trả 0)
Get-Process nginx -ErrorAction SilentlyContinue | Measure-Object | Select-Object -ExpandProperty Count
```

### Kill tất cả API Instances

```powershell
# Cách 1: Dùng script (khuyến nghị)
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Infrastructure\scripts"
.\stop-cluster.ps1

# Cách 2: Kill thủ công nếu script không hoạt động
# Xem các dotnet process đang chạy
Get-Process dotnet -ErrorAction SilentlyContinue | Format-Table Id, ProcessName, StartTime

# Kill từng PID cụ thể
Stop-Process -Id <PID> -Force

# Hoặc kill TẤT CẢ dotnet process (⚠️ sẽ kill cả Frontend nếu đang chạy)
Stop-Process -Name dotnet -Force
```

### Kill process đang chiếm port cụ thể

```powershell
# Tìm process đang dùng port 80 (Nginx)
netstat -ano | findstr ":80 "

# Tìm process đang dùng port 5001/5002/5003
netstat -ano | findstr ":5001 "

# Kill theo PID (số cuối cùng trong output netstat)
taskkill /PID <PID> /F
```

### Start lại sạch sau khi kill

```powershell
# 1. Start Nginx (CHỈ 1 LẦN)
cd D:\Downloads\nginx-1.28.2\nginx-1.28.2
start nginx

# 2. Start API cluster
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Infrastructure\scripts"
.\start-cluster.ps1
```

> ⚠️ **QUAN TRỌNG:** Chỉ chạy `start nginx` **MỘT LẦN DUY NHẤT**. Mỗi lần chạy thêm sẽ tạo thêm process zombie. Nếu cần apply config mới → dùng `.\nginx -s reload`, KHÔNG start thêm.

---

## Thứ Tự Khởi Động Lại

Khởi động theo thứ tự:

```powershell
# 1. Redis (cần chạy trước vì API kết nối lúc khởi động)
docker start redis

# 2. API Cluster
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Infrastructure\scripts"
.\start-cluster.ps1

# 3. Đợi 10 giây cho API ổn định
Start-Sleep -Seconds 10

# 4. Nginx
cd D:\Downloads\nginx-1.28.2\nginx-1.28.2
start nginx

# 5. Frontend
cd "c:\Users\Administrator\source\repos\Ebay_Seller_Clone\Ebay_Seller_Clone\Frontend\EbayClone.Frontend"
dotnet run --environment Production
```

---

## Xử Lý Lỗi Thường Gặp

| Lỗi | Nguyên nhân | Cách fix |
|---|---|---|
| `docker: command not found` | Docker Desktop chưa cài hoặc chưa start | Cài Docker Desktop + khởi động |
| `PONG` không trả về | Redis container chưa chạy | `docker start redis` |
| `nginx: [emerg] bind() to 0.0.0.0:80 failed` | Port 80 đã bị dùng (IIS, Skype, etc.) | Mở Services → dừng World Wide Web Publishing Service, hoặc đổi port trong nginx.conf sang 8080 |
| `502 Bad Gateway` | Tất cả API instances đều down | Start cluster lại |
| `connect() failed to 127.0.0.1:5001` | Instance 5001 chưa start | Check `Get-Process dotnet` |
| `The process cannot access the file...` | Port đang bị dùng bởi process cũ | `.\stop-cluster.ps1 -All` rồi start lại |
| Build failed | Code lỗi hoặc NuGet chưa restore | `dotnet restore` rồi `dotnet build` lại |

---

## Tóm Tắt Ports

| Service | Port | Vai trò |
|---|---|---|
| Nginx | 80 | Entry point — load balancer, reverse proxy |
| API Instance 1 | 5001 | Backend Kestrel |
| API Instance 2 | 5002 | Backend Kestrel |
| API Instance 3 | 5003 | Backend Kestrel |
| API (dev đơn) | 7250 | Backend Kestrel (khi không dùng Nginx) |
| Blazor Frontend | 7251 (HTTPS) | UI — mở browser vào đây |
| Redis | 6380 | Distributed Lock + Cache |
| SQL Server | 1433 | Database |

## Tóm Tắt 2 Chế Độ Chạy

| | Dev (không Nginx) | Production (có Nginx) |
|---|---|---|
| **Start** | `dotnet run` API + Frontend | Redis + start-cluster + Nginx + Frontend `--environment Production` |
| **Frontend truy cập** | `https://localhost:7251` | `https://localhost:7251` |
| **Frontend gọi API qua** | `https://localhost:7250` | `http://localhost` (Nginx) |
| **Load balancing** | Không | Có (3 instances) |
| **Rate limiting** | Không | Có |
| **Failover** | Không | Có |
