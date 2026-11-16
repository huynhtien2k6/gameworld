# Thế Giới Mở

Bản nền tảng game online bằng **.NET 10 + Entity Framework Core**, với giao diện web tiếng Việt cho người chơi và đội ngũ vận hành. Bản hiện tại chơi qua các hành động trên giao diện: khám phá, giao dịch, trồng trọt, nấu ăn và chiến đấu theo lượt. Đây là nền backend và bản chơi thử, chưa phải client thế giới 3D hoàn chỉnh.

## Chạy trên máy

Cần .NET SDK 10. Trong thư mục chứa `GameWorld.slnx`:

```powershell
dotnet restore GameWorld.slnx
dotnet build GameWorld.slnx --no-restore
dotnet run --project GameWorld.Api --launch-profile http
```

- Người chơi: <http://localhost:5162/>
- Trang admin: <http://localhost:5162/admin.html>
- Kiểm tra kết nối database: <http://localhost:5162/health>
- Mô tả API trong môi trường Development: <http://localhost:5162/openapi/v1.json>

Trong Visual Studio, mở `GameWorld.slnx`, chọn **GameWorld.Api** làm Startup Project và chạy profile **http**. Solution C++ cũ `thế giới mở.slnx` được giữ nguyên; solution .NET có đầy đủ ba project.

Database SQLite được tạo và điền dữ liệu thế giới ở lần chạy đầu, tại `GameWorld.Api/App_Data/gameworld.db`. Tài khoản, nhân vật, kho đồ, tiền, giao dịch và thao tác quản trị vẫn còn sau khi khởi động lại. Không cần cài SQL Server để thử bản này. Dữ liệu và khóa ký phát triển được loại khỏi Git bằng `.gitignore`.

## Tạo quản trị viên

Không có tài khoản admin hoặc mật khẩu mặc định. Dừng máy chủ hiện tại nếu đang chạy, rồi dùng:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start.ps1 -SetupAdmin
```

Script hỏi tên và mật khẩu ẩn, truyền qua môi trường của tiến trình và tạo admin nếu tên đó chưa tồn tại. Mật khẩu phải có ít nhất 12 ký tự và chỉ được lưu dưới dạng băm PBKDF2 trong database. Script không đổi mật khẩu admin đã tồn tại. Những lần sau chạy lệnh `dotnet run` như bình thường. Có thể dùng cấu hình `BootstrapAdmin__Username` và `BootstrapAdmin__Password` từ hệ thống quản lý bí mật thay cho script.

## Luồng chơi

1. Đăng ký tài khoản, đăng nhập và tạo nhân vật. Mọi nhân vật bắt đầu tại **Làng Tân Thủ**, với 5 lúa mì, 3 thịt; ví tài khoản mới có 100 vàng và dùng chung giữa các nhân vật.
2. Giao dịch với **Bà Lan**, học nấu ăn từ **Chủ Quán**, học trồng trọt từ **Bác Năm**. Chỉ tương tác được với NPC cùng bản đồ.
3. Trồng lúa tại làng, chờ đủ thời gian theo mùa/thời tiết rồi thu hoạch. Nấu 2 lúa mì thành bánh; dùng thức ăn để hồi máu.
4. Đến **Rừng Ven Làng** để đánh slime, nhận kinh nghiệm/vàng/chiến lợi phẩm, trở lại làng nghỉ ngơi. Trang bị kiếm để tăng sức chiến đấu. Các vùng núi và sa mạc yêu cầu cấp độ cao hơn.
5. Giá hàng thay đổi theo vùng, mùa, tồn kho NPC và biến động kinh tế do admin cấu hình. Mang hàng đến vùng khác, giao dịch với NPC, hoặc mở tin bán trên chợ người chơi. Hàng rao bán được giữ trong ký quỹ; mua bán chuyển vàng và hàng trong cùng giao dịch database.
6. Lập hoặc gia nhập thương đoàn với vai trò thành viên, lái xe, trinh sát, hộ vệ. Bản hiện tại có danh sách và vai trò; chưa mô phỏng hành trình đoàn xe hay chiến đấu hộ tống.
7. Kỹ năng học qua thực tế: 10 lần nấu, 50 giao dịch NPC thành công, 20 lần thu hoạch, 25 lần di chuyển. Khi đủ điều kiện, gặp đúng người dạy để mở kỹ năng và nhận hiệu ứng thực tế. Số đếm do server ghi nhận, không nhận từ client.

Thời tiết là mô phỏng trong game theo mùa, khí hậu và khung giờ, thống nhất cho mọi người chơi. Mưa, tuyết, sương mù và bão cát ảnh hưởng tốc độ di chuyển, chi phí đi lại, thời gian sinh trưởng và năng suất. Mùa hiện dùng quý theo UTC: Xuân tháng 1–3, Hè 4–6, Thu 7–9, Đông 10–12; giờ hiển thị theo UTC+7. Thời gian phát triển cây và sản lượng được chốt khi gieo, tránh đổi vùng để nhận thêm lợi ích.

## Trang quản trị

- Tra cứu tài khoản/nhân vật, ban có thời hạn hoặc vĩnh viễn, mở khóa. Ban thu hồi mọi phiên đăng nhập của tài khoản.
- Xem và trả lời yêu cầu hỗ trợ; đánh dấu đã giải quyết; khôi phục vật phẩm, cấp hoặc thu hồi vàng với lý do bắt buộc.
- Điều chỉnh giá gốc vật phẩm và trọng số rơi đồ Gacha. Vòng quay dùng 20 vàng kiếm trong game; tỷ lệ được công khai trước khi quay và kết quả do server chọn.
- Lên lịch, bật/tắt sự kiện, gửi thông báo toàn server và phát Giftcode tặng vàng/vật phẩm. Mỗi tài khoản nhận một lần, có hạn dùng và giới hạn tổng lượt nhận.
- Xem CCU theo heartbeat trong 2 phút gần nhất, số tài khoản, tổng vàng, số yêu cầu đang mở, doanh thu ghi nhận, lỗi và nhật ký thao tác admin.

Doanh thu hiện đọc từ bảng `DailyRevenues`; chưa nối cổng nạp tiền và chưa có webhook xác nhận thanh toán, nên database mới hiển thị **0**, không dùng số liệu giả. CCU là số tài khoản có phiên đang hoạt động và heartbeat gần đây, không phải số kết nối mạng thời gian thực. Thư toàn server là bảng thông báo chung, chưa có trạng thái đọc riêng từng người chơi. Sự kiện LiveOps hiện có lịch và trạng thái hiển thị; nội dung nhiệm vụ/phần thưởng riêng cho từng lễ hội cần được xây dựng thêm.

API `POST /api/admin/economy-events` nhận `regionId`, `itemId`, `modifier` (0.1–10), `eventType`, `description`, `startsAt`, `endsAt` để tạo biến động thị trường như vây hãm, mất mùa hoặc đứt tuyến thương mại. Hệ số cuối được giới hạn trong 0.1–10 lần giá gốc. Đây hiện là API quản trị; giao diện chưa có trình soạn thảo biến động này.

## Kiến trúc và an toàn dữ liệu

- `GameWorld.Domain`: thực thể người chơi, thế giới, kinh tế, hỗ trợ và vận hành.
- `GameWorld.Infrastructure`: `GameDbContext`, ràng buộc duy nhất, quan hệ và dữ liệu nền cố định.
- `GameWorld.Api`: REST API, xác thực, logic game và giao diện tĩnh tại `wwwroot`.
- JWT người chơi gắn phiên database; refresh token được băm và xoay vòng. Logout hoặc ban làm access token của phiên mất hiệu lực. Admin có JWT theo vai trò riêng và được kiểm tra trạng thái ở mỗi yêu cầu.
- Mật khẩu dùng ASP.NET Identity PBKDF2; tài khoản cũ dùng SHA256 được nâng cấp khi đăng nhập thành công. API không trả hash/salt hoặc toàn bộ đồ thị tài khoản.
- Server kiểm tra quyền sở hữu, vị trí, cấp độ, số dư, tồn kho và thời gian chờ. Các hoạt động thưởng/mua bán quan trọng dùng transaction; SQLite bản local giới hạn một luồng ghi trong tiến trình để tránh nhận thưởng trùng.
- Có giới hạn tần suất API và đăng nhập sai; thao tác admin được lưu vết cùng lý do khi thay đổi kinh tế/tài khoản.

SQL Server dùng EF migrations và tự áp dụng migration khi `Database:Initialize` bật. SQLite tiếp tục dùng `EnsureCreated` cho bản thử nghiệm/local; hai cách khởi tạo này không được dùng chung trên cùng một database. Migration SQL Server ban đầu nằm trong `GameWorld.Infrastructure/Migrations/SqlServer`.

Để áp dụng migration SQL Server thủ công, từ thư mục solution chạy:

```powershell
dotnet tool restore --tool-manifest .config/dotnet-tools.json
dotnet tool run dotnet-ef -- database update --project GameWorld.Infrastructure --startup-project GameWorld.Api --context GameDbContext
```

Để tạo migration sau khi thay đổi model, dùng `dotnet tool run dotnet-ef -- migrations add <MigrationName> --project GameWorld.Infrastructure --startup-project GameWorld.Api --context GameDbContext --output-dir Migrations/SqlServer`. Bài kiểm tra tích hợp vẫn chạy trên SQLite database tạm. Trước khi đưa lên máy chủ thật cần đặt `JwtSettings__Key` dài ít nhất 32 byte bằng hệ thống secret, cấu hình HTTPS, sao lưu, giám sát và quy trình triển khai.

## Kiểm tra

```powershell
dotnet build GameWorld.slnx --no-restore
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Script tích hợp tự khởi động API với database/credential kiểm thử riêng trong `artifacts`, kiểm tra quyền truy cập, phiên đăng nhập, giao dịch, hỗ trợ và LiveOps, rồi dừng tiến trình do nó tạo. Không chạy trên database mặc định của bạn. Công cụ kiểm tra trình duyệt của phiên phát triển này không kết nối được, nên chưa xác minh trực quan trên desktop/mobile bằng trình duyệt.

## Các phần tiếp theo của game hoàn chỉnh

Client 3D low-poly/cel-shading cho PC/mobile (Unity hoặc Godot), đồng bộ vị trí/chiến đấu thời gian thực, nhóm bạn và chat, bản đồ lớn với streaming, AI/NPC và nhiệm vụ phong phú, đoàn xe/đánh cướp tuyến đường, đấu giá/chợ đen/thuế bang hội, cửa hàng NPC bán offline, chống gian lận chuyên sâu, thanh toán và hạ tầng nhiều máy chủ chưa được triển khai đầy đủ trong bản nền tảng này.
