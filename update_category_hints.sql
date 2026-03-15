-- Cập nhật AttributeHints cho Categories đã tồn tại (không có hints)
UPDATE Categories 
SET AttributeHints = N'["Màu sắc","Dung lượng RAM","Bộ nhớ","Phần mềm"]' 
WHERE Slug = 'dien-thoai-phu-kien' AND (AttributeHints IS NULL OR AttributeHints = '');

UPDATE Categories 
SET AttributeHints = N'["Màu sắc","Chuỗi Size","Chất liệu"]' 
WHERE Slug = 'thoi-trang-nam' AND (AttributeHints IS NULL OR AttributeHints = '');

UPDATE Categories 
SET AttributeHints = N'["Màu sắc","Chuỗi Size","Kiểu dáng"]' 
WHERE Slug = 'thoi-trang-nu' AND (AttributeHints IS NULL OR AttributeHints = '');

UPDATE Categories 
SET AttributeHints = N'["Màu sắc","Chất liệu","Kích thước"]' 
WHERE Slug = 'nha-cua-doi-song' AND (AttributeHints IS NULL OR AttributeHints = '');

SELECT Id, Name, Slug, AttributeHints FROM Categories ORDER BY Name;
