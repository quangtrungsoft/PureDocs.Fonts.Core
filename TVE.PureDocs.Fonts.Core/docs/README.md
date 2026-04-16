# TVE.PureDocs.Fonts.Core

**TVE.PureDocs.Fonts.Core** là một thư viện .NET hiệu năng cao, được thiết kế để phân tích (parse), trích xuất thông tin (metrics extractor) và tạo tập con (subsetting) cho các tệp font TrueType (TTF) và OpenType (OTF).

Thư viện này được xây dựng với triết lý **"Zero third-party dependencies"** (không phụ thuộc vào bên thứ ba) và sử dụng tối ưu bộ nhớ thông qua `ReadOnlyMemory<byte>` và `Span<byte>`.

## 🎯 Mục đích sử dụng

- **Phân tích tệp Font**: Đọc thông tin cấu trúc, bảng dữ liệu từ các tệp `.ttf` và `.otf`.
- **Trích xuất Typography Metrics**: Lấy thông tin về độ rộng ký tự (advance width), chiều cao (ascent, descent), khoảng cách dòng (line gap) và các cặp kerning để phục vụ việc dàn trang (layout engine).
- **Tạo Font Subset**: Tạo ra một tệp font mới chỉ chứa các ký tự cần thiết. Điều này cực kỳ quan trọng khi nhúng font vào PDF hoặc truyền tải qua web để giảm thiểu dung lượng file.
- **Nền tảng cho Rendering**: Cung cấp dữ liệu nền tảng cho các thư viện render PDF hoặc engine đồ họa.

## 🏗️ Kiến trúc hệ thống

Dự án được tổ chức theo các lớp chức năng tách biệt:

### 1. Lớp Dữ liệu (Tables)

Chứa các định nghĩa và logic phân tích cho từng bảng (Table) tiêu chuẩn của định dạng TrueType/OpenType:

- `HeadTable`: Thông tin chung về font (unitsPerEm, checksum...).
- `HheaTable` & `HmtxTable`: Thông tin về số liệu ngang (Horizontal Metrics).
- `GlyfTable` & `LocaTable`: Chứa dữ liệu glyph (đường bao) và vị trí của chúng.
- `CmapTable`: Bảng ánh xạ từ mã ký tự (Unicode) sang chỉ số glyph (Glyph ID).
- `NameTable`: Chứa các chuỗi tên font (Family Name, Full Name...).
- `PostTable`: Thông tin cho máy in PostScript.
- `Os2Table`: Thông tin đặc thù cho Windows và các thông số font chuẩn.

### 2. Lớp Hạ tầng (Parsing)

- `TtfFontReader`: Điểm vào (Entry point) để chuyển dữ liệu nhị phân thô thành đối tượng `TtfFontData`.
- `TtfFontData`: Đối tượng bất biến (Immutable) đại diện cho toàn bộ dữ liệu font đã được xử lý, cho phép truy cập nhanh vào các bảng và thông tin metrics.

### 3. Lớp Tính năng (Functional Layer)

- `Metrics`: Trích xuất và chuẩn hóa các thông số typography sang hệ tọa độ đơn vị.
- `Mapping`: Xử lý việc ánh xạ ký tự và quản lý Glyph ID.
- `Subsetting`: Logic cốt lõi để thu gọn font, bao gồm việc tính toán lại offset, checksum và tái cấu trúc các bảng dữ liệu.

## 📚 Định nghĩa khái niệm

- **Glyph**: Hình ảnh hiển thị của một ký tự. Một font là bộ sưu tập các glyph.
- **Glyph ID (GID)**: Chỉ số thứ tự của glyph trong tệp font.
- **Units Per Em**: Đơn vị đo lường nội bộ của font (thường là 1024 hoặc 2048). Tất cả thông số metrics đều dựa trên đơn vị này.
- **Kerning**: Việc điều chỉnh khoảng cách giữa các cặp ký tự cụ thể để trông tự nhiên hơn (ví dụ: cặp "AV").
- **Subsetting**: Quá trình trích xuất các glyph cần thiết để tạo ra một file font nhỏ hơn, chỉ chứa những gì cần dùng.

## 🚀 Hướng dẫn sử dụng nhanh

### Đọc thông tin Font

```csharp
using TVE.PureDocs.Fonts.Core.Parsing;

// Parse từ file hoặc mảng byte
var fontData = TtfFontReader.ParseFile("Roboto-Regular.ttf");

Console.WriteLine($"Font Family: {fontData.FamilyName}");
Console.WriteLine($"Units Per Em: {fontData.UnitsPerEm}");
```

### Tạo Font Subset

```csharp
using TVE.PureDocs.Fonts.Core.Subsetting;

var subsetter = new TtfSubsetter(fontData);
var result = subsetter.CreateSubset("Hello World");

// Lưu file font mới đã thu gọn
File.WriteAllBytes("Roboto-Subset.ttf", result.SubsetBytes);
```

## 🛠️ Yêu cầu hệ thống

- .NET 6.0 trở lên.
- Hỗ trợ đa nền tảng (Windows, Linux, macOS).

---

© 2024 TVE Open Source Project. Licensed under the MIT License.
