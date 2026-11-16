# Chạy bản thử nghiệm trên một máy Linux

Đây là cấu hình một máy chủ, một tiến trình API và SQLite bền vững trong Docker
volume. Chưa phải hạ tầng MMORPG đông người chơi. Không chạy nhiều bản sao API
trên cùng database SQLite. Các cập nhật schema SQLite cần quy trình migration
riêng: EnsureCreated không cập nhật schema của database cũ.

## AWS và ngân sách

Project hiện dùng **ap-southeast-2 (Sydney)**. Chỉ tạo tài nguyên trong region này.
Ngân sách mục tiêu 22 USD/tháng, không phải cam kết hoặc giới hạn hóa đơn.
Kiểm tra plan, dịch vụ được hỗ trợ và giá trong project trước khi tạo máy.
Lightsail ở Singapore từng bị SCP từ chối; điều đó chưa chứng minh dịch vụ bị
chặn tại Sydney. Không sửa SCP hoặc đổi region để tìm cách vượt hạn chế.

Trong CloudShell chạy các lệnh chỉ đọc sau, gửi kết quả không chứa bí mật:

```bash
aws freetier get-account-plan-state --region ap-southeast-2
aws lightsail get-bundles --region ap-southeast-2 --query 'bundles[?isActive && price<=`15`].[bundleId,price,ramSizeInGb,supportedPlatforms]' --output table
```

Chỉ chọn gói Linux sau khi đọc giá thực tế; ưu tiên RAM ít nhất 2 GB nếu có gói
phù hợp. Dành phần ngân sách còn lại cho backup, thuế và lưu lượng vượt định mức.
Nếu Lightsail không khả dụng, đánh giá EC2/EBS/IPv4 và lưu lượng bằng giá Sydney
trước khi tạo. Không tự triển khai ALB, NAT Gateway hoặc RDS trong ngân sách này.
Thiết lập cảnh báo ở 10/15/20 USD trong AWS Budgets. Kiểm tra spend limit của
project trong AWS Settings; cảnh báo budget không tự dừng tài nguyên.

## Chuẩn bị máy

Cần Linux x86_64/ARM64, Docker Engine, Docker Compose v2, Git, OpenSSL và domain
trỏ đúng IP máy chủ. Không dùng Docker Desktop trên Windows cho cấu hình host
network này. Tạo máy chỉ sau khi đã xác nhận dịch vụ và giá.

Firewall AWS mở TCP 80 và 443; TCP 22 chỉ từ IP quản trị của bạn. Không mở 8080
hoặc cổng database. Gắn IP tĩnh nếu dịch vụ hỗ trợ và kiểm tra chi phí IP.

```bash
git clone https://github.com/huynhtien2k6/gameworld.git
cd gameworld
bash deploy/init-env.sh
docker compose --project-directory deploy -f deploy/compose.yaml build
docker compose --project-directory deploy -f deploy/compose.yaml up -d
```

Build .NET có thể tốn RAM; nếu máy nhỏ, build image trên máy khác cùng kiến trúc
rồi chuyển image tới server. Caddy cấp HTTPS khi DNS, cổng 80/443 và domain hợp lệ.
API chỉ nghe loopback; chỉ tin forwarded headers từ proxy loopback, không tin
header giả do client bên ngoài gửi.

Truy cập `https://<domain>/`, `/play.html`, `/admin.html` và `/health`.
Đọc `ADMIN_PASSWORD` trong `deploy/.env` trực tiếp trên máy để đăng nhập admin;
không đưa nội dung file vào chat hoặc Git. Không có tài khoản admin mặc định
trước khi bootstrap. Thay biến bootstrap không đổi mật khẩu admin đã tồn tại.

## Backup và cập nhật

Sao lưu volume `game_data` trước cập nhật. Với SQLite, dừng API trong lúc sao lưu
hoặc dùng SQLite online backup; không copy riêng file .db đang ghi rồi bỏ WAL.
Lưu backup được mã hóa ngoài máy và thử khôi phục. `docker compose down` giữ volume;
**không dùng `down -v`**, lệnh đó xóa dữ liệu người chơi và chứng chỉ.

Trước khi cập nhật, ghi lại commit/image cũ, backup database, kiểm tra schema
tương thích rồi build lại và `up -d`. Khôi phục cả image và database khi rollback
có thay đổi schema. Theo dõi dung lượng ổ đĩa, CPU/RAM, lỗi và health endpoint;
chưa có autoscale, HA hoặc giám sát từ bên ngoài trong cấu hình này.
