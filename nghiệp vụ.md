TÀI LIỆU ĐẶC TẢ LUỒNG NGHIỆP VỤ - PHÂN HỆ NGƯỜI BÁN (SELLER HUB)
Dự án: eBay Clone Đối tượng: Team Phát triển (Backend, Frontend, QA)
________________


PHẦN 1: LUỒNG KHỞI TẠO & CẤU HÌNH CỬA HÀNG (ONBOARDING & SETTINGS - CHUẨN EBAY 2024 MVP)
Đây là bước đầu tiên khi một User bình thường muốn trở thành Người bán chuyên nghiệp trên sàn. (Rút gọn luồng xác minh KYC để tập trung vào vòng đời Seller thực tế).
1. Luồng Xác minh nhanh & Mở Shop (MVP):
* Bước 1: User gửi yêu cầu mở Shop (Mặc định chọn tài khoản loại Personal). Không làm phức tạp luồng Doanh nghiệp ở scope hiện tại.
* Bước 2: Hệ thống tạo bản ghi trong bảng Shops với trạng thái IsVerified = False, IsIdentityVerified = False.
* Bước 3 (Xác minh định danh): Gọi SMS gửi OTP số điện thoại. (MVP: chỉ cần nhập mã OTP ảo 123456).
  - Khi OTP đúng: set IsIdentityVerified = True + Khởi tạo ví rỗng SellerWallet atomic.
  - LƯU Ý: IsVerified vẫn = False! User chưa được tạo Policy hay List item.
* Bước 4 (Financial Info - Link Bank Account):
  - User nhập Routing Number + Account Number. Hệ thống sinh 2 micro-deposit ngẫu nhiên (0.01-0.99$).
  - BankVerificationStatus = "Pending". User chờ 1-4 ngày.
* Bước 5 (Verify Micro-Deposits):
  - User nhập lại 2 số tiền micro-deposit. So khớp thành công → BankVerificationStatus = "Verified".
  - NẾU IsIdentityVerified = True → set IsVerified = True + Seed Default Policies (Shipping, Return, Payment).
  - Giới hạn 3 lần thử, sai quá → khóa.
* Bước 6 (Go Live): Shop đã IsVerified = True. User có thể:
  - Truy cập Business Policies (đã có 3 default policies được seed).
  - Bắt đầu tạo Listing.
  - MonthlyListingLimit mặc định = 250 sản phẩm/tháng cho free tier.
2. Luồng Quản lý Hồ sơ & Trạng thái hoạt động (Store Profile & Context):
* Chuyển đổi tài khoản (Switch Mode): Nút "Switch to Selling" cho phép User đang ở chế độ Buyer chuyển sang giao diện Dashboard của Seller (load lại UI kèm quyền Seller).
* Cập nhật Hồ sơ Shop: Seller tải lên Banner, Avatar, sửa Tên Shop và Mô tả.
3. Luồng Thiết lập Chính sách Bắt buộc (Business Policies):
Hàng hóa không được phép tự do nhập giá ship lẻ tẻ. Bắt buộc tạo kho Policy tập trung.
* Payment Policy: Tạo policy yêu cầu thanh toán (vd: "Require immediate payment" để chống ngâm đơn).
* Return Policy: Tạo policy cho phép trả hàng (14/30/60 ngày), ai chịu phí ship trả (Buyer/Free returns), tự động đồng ý trả hàng.
* Shipping Policy: Tạo policy cài đặt Handling Time (vd: 1-3 ngày), loại hình ship (Flat, Calculated, Freight), Dịch vụ ship (USPS, UPS, FedEx). Gồm cả tuỳ chọn bật eBay International Shipping (EIS).
________________


PHẦN 2: LUỒNG QUẢN LÝ NGUỒN CUNG (LISTING & INVENTORY FLOW - CHUẨN EBAY 2024)
Nơi Seller quản lý hàng hóa hiển thị trên sàn. Listing = 1 Product + N Variants (SKUs). Hệ thống hỗ trợ Đấu giá (Auction) và Giá cố định (Fixed Price), kèm Best Offer, Item Specifics bắt buộc, và quản lý trạng thái tồn kho tự động.

1. Hệ thống Danh mục & Seed Data (Category & Item Specifics):
   * Danh mục phân cấp 2 tầng: Root (Electronics, Clothing & Accessories...) → Sub (Cell Phones, Men's Shirts...). Mỗi Category có Slug, AttributeHints (gợi ý thuộc tính biến thể), và ParentId.
   * CategorySeeder chạy idempotent khi app khởi động (Program.cs) — seed 6 root + 12 sub categories + 60+ CategoryItemSpecifics.
   * Mỗi CategoryItemSpecific gồm: Name (vd: "Brand"), Requirement (REQUIRED/RECOMMENDED/OPTIONAL), SuggestedValues (vd: "Apple,Samsung,Google"), SortOrder.
   * LƯU Ý: Controller KHÔNG chứa inline seed data — toàn bộ seed nằm ở CategorySeeder.

2. Luồng Đăng bán Sản phẩm (Create Listing):
   * Bước 1 (Thông tin cơ bản):
      - Nhập Title (3-255 ký tự), Subtitle (max 80 ký tự, hiển thị dưới tiêu đề trong kết quả tìm kiếm, phí phụ trên eBay thật), Description, Brand.
      - Chọn Danh mục (**bắt buộc**): Dropdown hiển thị cấu trúc phân cấp (└ Sub-category). Khi chọn → API tự động load Item Specifics: GET /api/categories/{id}/item-specifics.

   * Bước 2 (Condition — ở Product Level):
      - **BẮT BUỘC** chọn Condition. Giá trị hợp lệ: New, New Other, Open Box, Seller Refurbished, Used, For Parts.
      - LƯU Ý: Condition lưu ở Product (KHÔNG phải ở Variant). Tất cả variants cùng listing phải cùng 1 Condition (đúng chuẩn eBay).
      - Có thể thêm ConditionDescription mô tả chi tiết (vết xước, thiếu phụ kiện...).

   * Bước 3 (Listing Format & Best Offer):
      - Chọn hình thức bán:
         a. FIXED_PRICE (Giá cố định, mặc định): Bán ngay với giá niêm yết. Hỗ trợ nhiều biến thể.
         b. AUCTION (Đấu giá): Listing dạng đấu giá. **BẮT BUỘC chỉ có đúng 1 variant** — backend reject nếu >1.
      - Best Offer (AllowOffers):
         a. Bật → Buyer có thể đặt giá. Seller duyệt/từ chối thủ công.
         b. AutoAcceptPrice: Tự chấp nhận offer ≥ X (vd: ≥ 900K → tự đồng ý).
         c. AutoDeclinePrice: Tự từ chối offer < Y (vd: < 500K → tự reject).
         d. Validate: AutoAcceptPrice PHẢI > AutoDeclinePrice (nếu cả 2 được set).

   * Bước 4 (Item Specifics — Trọng yếu cho SEO):
      - Hiển thị form động theo Category đã chọn (load từ API):
         a. REQUIRED (đỏ *): Bắt buộc điền TRƯỚC khi đăng bán. Nếu thiếu → backend reject status ACTIVE.
         b. RECOMMENDED (vàng): Nên điền để tăng khả năng xuất hiện trong bộ lọc.
         c. OPTIONAL (xám): Tùy chọn.
      - Nếu có SuggestedValues → hiển thị dropdown. Nếu không → input text tự do.
      - LƯU Ý: Item Specifics được validate khi chuyển status DRAFT/SCHEDULED → ACTIVE (publish). Khi tạo nháp thì không bắt buộc.
      - Lưu vào bảng ProductItemSpecifics (ProductId, Name, Value).

   * Bước 5 (Upload ảnh):
      - 1 ảnh bìa (PrimaryImageUrl) + tối đa 5 ảnh phụ (ImageUrls lưu JSON array). Giới hạn hardcode ở FE.
      - Mỗi biến thể có thể có ảnh riêng (variant.ImageUrl).
      - Upload qua FileUploadService → POST /api/files/upload.

   * Bước 6 (Biến thể — Variations/SKUs):
      - Mỗi listing BẮT BUỘC ít nhất 1 variant. Mỗi variant gồm: SkuCode (unique), Price, Quantity, Attributes (động dạng Key-Value), WeightGram (tùy chọn).
      - **Giới hạn cứng (Validation ở Application Layer):**
         a. Tối đa **250 variants** / listing.
         b. Tối đa **5 thuộc tính** / variant (vd: Color, Size, Storage, RAM, Material).
         c. Tối đa **50 options** / thuộc tính (vd: Color có max 50 màu khác nhau).
      - Thuộc tính lưu dual-store: JSON snapshot (quick read) + VariantAttributeValues relational table (query/filter).

   * Bước 7 (Chính sách & Xuất bản):
      - Chọn 3 Policy (Shipping, Return, Payment) từ kho đã tạo ở Phần 1. Nếu không chọn → hệ thống tự gán Default Policy.
      - Xuất bản: 3 nút:
         a. "💾 Lưu Nháp" → Status = DRAFT (default từ backend).
         b. "⏰ Hẹn Giờ Đăng" → Status = SCHEDULED (cần set ScheduledAt).
         c. "🚀 Đăng Bán Ngay" → Tạo DRAFT → gọi PATCH /api/products/{id}/status thành ACTIVE.
      - Khi publish (ACTIVE): Backend validate REQUIRED Item Specifics. Thiếu → reject 400 kèm danh sách cần điền.

3. Luồng Quản lý Trạng thái Sản phẩm (Status Lifecycle):
   * Các trạng thái hợp lệ: DRAFT, ACTIVE, SCHEDULED, OUT_OF_STOCK, HIDDEN, ENDED.
   * Diagram chuyển trạng thái:
      - DRAFT → ACTIVE (sau validate Item Specifics) | SCHEDULED (có ScheduledAt)
      - SCHEDULED → ACTIVE (khi đến giờ — cần Background Job)
      - ACTIVE → HIDDEN (seller ẩn thủ công) | OUT_OF_STOCK (tự động, xem mục 4) | ENDED (seller kết thúc)
      - OUT_OF_STOCK → ACTIVE (tự động khi restock) | HIDDEN (seller ẩn) | ENDED (seller kết thúc)
      - HIDDEN → ACTIVE (seller mở lại) | ENDED (seller kết thúc)
      - **ENDED → ⛔ KHÔNG THỂ chuyển sang trạng thái khác** (final state — phải tạo listing mới = relist)
   * LƯU Ý: OUT_OF_STOCK là trạng thái TỰ ĐỘNG — seller KHÔNG THỂ set manual. Chỉ hệ thống chuyển khi tất cả variants hết hàng.
   * LƯU Ý: ENDED là trạng thái CUỐI CÙNG — seller có thể kết thúc listing thủ công bất kỳ lúc nào (giống nút "End listing" trên eBay).
   * Tại sao dùng OUT_OF_STOCK thay vì HIDDEN? Vì eBay giữ SEO ranking cho listing OUT_OF_STOCK, còn HIDDEN thì mất ranking.
   * [CHƯA IMPLEMENT] Auto-end Auction: Background Job tự động chuyển ENDED khi Auction hết hạn thời gian đấu giá — dự kiến phase khi có module Auction bidding.

4. Luồng Quản lý Tồn kho & Tự động OUT_OF_STOCK (Inventory & A6):
   * Cập nhật tồn kho (Restock): Seller nhập thêm số lượng qua Inventory page. Gọi PUT /api/products/variants/{id}/restock.
   * Tuyệt đối không bù trừ vào ReservedQuantity — chỉ cộng Quantity.
   * **Cơ chế tự động OUT_OF_STOCK (Domain Logic: Product.CheckAndUpdateStockStatus()):**
      - Sau MỌI thao tác ảnh hưởng stock (restock, deduct kho khi PAID, trả hàng khi CANCELLED):
         a. Nếu Status = ACTIVE && TẤT CẢ variants có (Quantity - ReservedQuantity) ≤ 0 → chuyển OUT_OF_STOCK.
         b. Nếu Status = OUT_OF_STOCK && CÓ BẤT KỲ variant nào (Quantity - ReservedQuantity) > 0 → chuyển lại ACTIVE.
      - Gọi từ: RestockVariantUseCase, UpdateOrderStatusUseCase (READY_TO_SHIP + CANCELLED).
   * Trang Restock/Inventory: FE đã có trang quản lý tồn kho tại /listings/inventory. Seller có thể xem trạng thái kho, lọc biến thể hết/sắp hết, và nhập kho (Restock) inline. Backend endpoint PUT /api/products/variants/{id}/restock đã sẵn sàng.

5. Luồng Quản lý Danh sách & Sửa/Xóa (Listing Maintenance):
   * Sửa toàn diện (Full Update): PUT /api/products/{id}/full — sửa tất cả: tên, ảnh, variants, condition, format, specifics, policies. Dùng Concurrency Token (RowVersion) chống xung đột.
   * Sửa biến thể (Variant Sync): DELETE variants cũ không còn trong request + UPDATE existing + ADD mới. Giải quyết Ghost Variants (variant mồ côi trong DB).
   * Tạm ẩn: Đổi Status = HIDDEN. Sản phẩm biến mất khỏi search nhưng vẫn tồn tại trong DB.
   * Xóa mềm: IsDeleted = True, sản phẩm biến mất khỏi mọi giao diện. Tuyệt đối cấm Hard Delete bằng SQL DELETE để bảo toàn lịch sử đơn hàng.
   * LƯU Ý (Data Loss Risk): Khi sửa listing, FE PHẢI load toàn bộ fields từ product trước khi submit. Nếu không → default values đè mất data gốc (vd: ListingFormat reset về FIXED_PRICE, AllowOffers reset về false).
________________


PHẦN 3: LUỒNG VẬN HÀNH ĐƠN HÀNG (ORDER FULFILLMENT FLOW — CHUẨN EBAY 2024)
Xương sống của hệ thống. Dự án tập trung vai trò SELLER — buyer actions được mock/giả lập, chỉ document nơi seller thực sự tương tác.

=== ORDER STATUS LIFECYCLE (State Machine) ===
Các trạng thái: PENDING_PAYMENT, PAID, SHIPPED, DELIVERED, CANCELLED, RETURN_REQUESTED, RETURN_IN_PROGRESS, REFUNDED, PARTIALLY_REFUNDED, DISPUTE_OPENED, COMPLETED.
Chuyển trạng thái:
   - PENDING_PAYMENT → PAID (mock: buyer pay) | CANCELLED (timeout 4 ngày / buyer cancel)
   - PAID → SHIPPED (**seller** upload tracking) | CANCELLED (**seller** cancel / buyer cancel request)
   - SHIPPED → DELIVERED (hệ thống giả lập carrier, seller KHÔNG tự mark)
   - DELIVERED → COMPLETED (hệ thống auto, hết return window)
   - DELIVERED → RETURN_REQUESTED (mock: buyer mở return) | DISPUTE_OPENED (mock: buyer mở case)
   - RETURN_REQUESTED → RETURN_IN_PROGRESS (**seller** accept) | PARTIALLY_REFUNDED (**seller** offer partial) | COMPLETED (**seller** decline) | DISPUTE_OPENED (buyer escalate)
   - RETURN_IN_PROGRESS → REFUNDED (**seller** inspect + refund) | PARTIALLY_REFUNDED (**seller** deduct max 50%)
   - DISPUTE_OPENED → COMPLETED (seller win) | REFUNDED (buyer win)
   - CANCELLED, COMPLETED, REFUNDED, PARTIALLY_REFUNDED → ⛔ FINAL STATES
   LƯU Ý:
   - READY_TO_SHIP đã bỏ (eBay thật không có).
   - DELIVERED chỉ do HỆ THỐNG chuyển — seller TUYỆT ĐỐI KHÔNG tự mark (chống gian lận).

0. Mock: Tạo Đơn Hàng (Buyer-side — Giả lập):
   * Đầu vào mock: BuyerId, VariantId, Quantity, ShippingAddress. Hệ thống tự xử lý:
      - Trừ kho 1 bước: UPDATE ProductVariants SET Quantity = Quantity - @qty WHERE Id = @id AND Quantity >= @qty.
      - KHÔNG dùng ReservedQuantity 2 bước (eBay thật không dùng, gây race condition).
      - Nếu 0 rows affected → reject "Out of stock".
      - Tạo Order (Status = PAID), OrderItems (PriceAtPurchase), ShipByDate = NOW + HandlingTime.
      - CheckAndUpdateStockStatus() → auto OUT_OF_STOCK nếu hết hàng.
      - PendingBalance += TotalAmount. Ghi WalletTransaction.
   * MVP: Không cần build checkout UI. Tạo API mock "Place Order" để seed data test.

1. Luồng Xử lý của Seller (Fulfillment — CORE):
   * Seller thấy đơn trong Seller Hub → Orders → Tab "Awaiting Shipment" (status = PAID).
   * Ship-by Date: PaymentDate + HandlingTime (từ Shipping Policy). Trễ → Late Shipment defect (PHẦN 7).
   * **Seller Actions:**
      a. **Add Tracking Number**: Nhập tracking # + carrier → Order → SHIPPED.
      3. Luồng Return/Refund (Seller respond — QUAN TRỌNG):
   * Mock trigger: Buyer mở Return Request (DELIVERED, trong return policy 14/30/60 ngày, có Reason + ảnh).
   * Order → RETURN_REQUESTED.
   * **Seller PHẢI respond trong 3 BUSINESS DAYS. Các options:**
      a. **Accept Return**: Buyer ship back (max 15 ngày) → RETURN_IN_PROGRESS → Seller nhận hàng → inspect (2 ngày) → Issue Refund. NẼU seller không refund trong 2 ngày → HỆ THỐNG AUTO REFUND.
      b. **Offer Partial Refund**: Return → PARTIAL_OFFERED (chờ buyer). Buyer chọn:
         - ACCEPT → trừ tiền ví (OnHold→Pending→Available fallback) → PARTIALLY_REFUNDED.
         - REJECT → quay lại REQUESTED (seller offer lại hoặc buyer escalate dispute).
      c. **Full Refund — Buyer Keep Item**: Buyer giữ hàng + full refund (cho hàng giá thấp). Trừ tiền ngay.
      d. **Decline Return**: Chỉ khi policy "No Returns" + buyer đổi ý. NHƯNG: Money Back Guarantee override → SNAD/Damaged → PHẢI accept.
   * After Return:
      - Order → REFUNDED / PARTIALLY_REFUNDED.
      - **Seller quyết định stock**: Hàng OK → Quantity += ReturnedQty. Damaged → seller decide.
      - Finance: Trừ tiền theo fallback 3 lớp: OnHoldBalance → PendingBalance → AvailableBalance (cho phép âm nếu cần).
   * Rules:
      - Money Back Guarantee: "No Returns" vẫn phải accept nếu SNAD/Damaged.
      - Return Shipping: Buyer đổi ý → theo policy. SNAD → seller LUÔN trả.
      - Free Returns Deduction: Seller deduct tối đa 50% nếu hàng damaged.
      - KHÔNG CÓ Restocking Fee.�y.
   * 2c. Seller Cancel — Out of Stock:
      - Reason: "Out of stock" → CANCELLED, refund buyer.
      - ⚠️ Seller bị GHI 1 TRANSACTION DEFECT. Rate > 2% → Below Standard.
   * 2d. Seller Cancel — Address Issue:
      - Reason: "Address issue" → CANCELLED, refund buyer, KHÔNG ảnh hưởng metrics.
   * Cancel Reasons & Impact:
      - "Buyer asked"   → ✅ Không ảnh hưởng seller.
      - "Hasn't paid"   → ✅ Fee credit.
      - "Out of stock"  → ❌ DEFECT.
      - "Address issue"  → ✅ Không ảnh hưởng.
   * Stock Restore: Nếu đã trừ kho (qua PAID) → Quantity += CancelledQty + CheckAndUpdateStockStatus().

3. Luồng Return/Refund (Seller respond — QUAN TRỌNG):
   * Mock trigger: Buyer mở Return Request (DELIVERED, trong return policy 14/30/60 ngày, có Reason + ảnh).
   * Order → RETURN_REQUESTED.
   * **Seller PHẢI respond trong 3 BUSINESS DAYS. Các options:**
      a. **Accept Return**: Buyer ship back (max 15 ngày) → RETURN_IN_PROGRESS → Seller nhận hàng → inspect (2 ngày) → Issue Refund. NẾU seller không refund trong 2 ngày → HỆ THỐNG AUTO REFUND.
      b. **Offer Partial Refund**: Buyer giữ hàng + nhận 1 phần tiền. Chỉ offer 1 LẦN. Buyer reject → có thể escalate.
      c. **Full Refund — Buyer Keep Item**: Buyer giữ hàng + full refund (cho hàng giá thấp).
      d. **Decline Return**: Chỉ khi policy "No Returns" + buyer đổi ý. NHƯNG: Money Back Guarantee override → SNAD/Damaged → PHẢI accept.
   * After Return:
      - Order → REFUNDED / PARTIALLY_REFUNDED.
      - **Seller quyết định stock**: Hàng OK → Quantity += ReturnedQty. Damaged → seller decide.
      - Finance: PendingBalance -= RefundAmount.
   * Rules:
      - Money Back Guarantee: "No Returns" vẫn phải accept nếu SNAD/Damaged.
      - Return Shipping: Buyer đổi ý → theo policy. SNAD → seller LUÔN trả.
      - Free Returns Deduction: Seller deduct tối đa 50% nếu hàng damaged.
      - KHÔNG CÓ Restocking Fee.

4. Luồng Khiếu nại (Dispute — Seller respond):
   * Mock trigger: Buyer mở Case (INR: 30 ngày sau delivery / SNAD: hàng sai mô tả).
   * Order → DISPUTE_OPENED. HoldForDispute(): tiền chuyển từ Pending/Available → **OnHoldBalance** (lock thực sự, seller không rút được).
   * **Seller có 3 business days để respond** (nộp bằng chứng: tracking, ảnh đóng hàng).
   * Nếu không resolve → Platform review (giả lập).
   * Kết quả:
      - Buyer win → ProcessRefund(): trừ từ OnHold→Pending→Available (fallback). Order → REFUNDED, seller bị 1 DEFECT.
      - Seller win → ProcessRelease(): giải ngân từ OnHold → Available (trừ phí sàn). Order → COMPLETED.

5. Stock Management (Hệ thống tự xử lý, nhưng seller cần hiểu):
   * Trừ kho: Atomic UPDATE khi PAID (1 bước).
   * Cộng kho: Khi CANCELLED (sau PAID) hoặc Return completed (seller decide).
   * Auto OUT_OF_STOCK / ACTIVE: CheckAndUpdateStockStatus() sau MỌI thao tác stock.

6. Liên kết Finance (PHẦN 4):
   * PAID → PendingBalance += Amount. Ghi WalletTransaction (ORDER_INCOME).
   * DISPUTE_OPENED → HoldForDispute(): Pending/Available → OnHoldBalance. (lock tiền).
   * CANCELLED/REFUNDED → ProcessRefund(): trừ từ OnHold → Pending → Available (fallback, cho âm).
   * COMPLETED (seller win / release) → ProcessRelease(): OnHold/Pending → Available (trừ phí sàn).
   * BalanceAfter: Tất cả log dùng TotalBalance = Available + Pending + OnHold.
________________


PHẦN 4: LUỒNG DÒNG TIỀN & ĐỐI SOÁT (FINANCE & ESCROW FLOW — ĐÃ REDESIGN)
Đảm bảo an toàn tài chính cho cả sàn và người dùng. Ví seller có 3 loại số dư:
   * AvailableBalance: Tiền sạch, seller có thể rút.
   * PendingBalance: Tiền chờ xử lý (chưa đủ thời gian hold).
   * OnHoldBalance: Tiền bị lock (dispute, return, chờ giải quyết).
   * TotalBalance = Available + Pending + OnHold (dùng cho BalanceAfter log).

1. Luồng Ghi nhận Doanh thu (PAID):
   * Khi khách thanh toán $100: PendingBalance += 100.
   * Ghi WalletTransaction: Type="ORDER_INCOME", Amount=+100, BalanceAfter=TotalBalance.
   * Tiền chưa vào Available ngay — phải chờ hold period (theo SellerLevel).

2. Luồng Hold theo SellerLevel (Escrow):
   * Thời gian hold phụ thuộc mức tài khoản seller:
      - NEW: 21 ngày (giữ lâu nhất, bảo vệ sàn).
      - BELOW_STANDARD: 14 ngày.
      - ABOVE_STANDARD: 3 ngày.
      - TOP_RATED: 0 ngày (tiền vào Available ngay).
   * [CHƯA IMPLEMENT] Background Job tự động release EscrowHold khi hết hạn HoldReleasesAt.

3. Luồng Giải ngân (Release Funds — ProcessRelease):
   * Khi đơn đạt COMPLETED (hết hold period hoặc dispute seller win):
      - Trừ phí sàn: profit = TotalAmount - PlatformFee.
      - ProcessRelease(totalAmount, profit): Pending -= totalAmount, Available += profit.
      - Fallback: Nếu Pending không đủ → trừ OnHold → cho âm (không crash).
      - Ghi WalletTransaction: Type="ESCROW_RELEASE", Amount=+profit.

4. Luồng Hoàn tiền (Refund — ProcessRefund):
   * Khi return/dispute buyer win/cancel:
      - ProcessRefund(refundAmount): trừ theo thứ tự OnHold → Pending → Available.
      - Cho phép ví âm nếu không đủ tiền (giống eBay thật).
      - Return breakdown log: "Hold: -50, Pending: -30, Available: -20".
      - Ghi WalletTransaction: Type="REFUND", Amount=-refundAmount.

5. Luồng Dispute Hold (HoldForDispute):
   * Khi buyer mở dispute: HoldForDispute(orderAmount).
      - Chuyển tiền từ Pending/Available → OnHoldBalance.
      - Seller không thể rút tiền đơn này cho đến khi resolve.
   * Seller win → ProcessRelease() (giải ngân).
   * Buyer win → ProcessRefund() (hoàn tiền).

6. Trường hợp Ví Âm (Negative Balance):
   * Xảy ra khi: refund lớn hơn số dư, phí sàn vượt available, timing payout/deduction.
   * Hệ thống cho phép âm — không crash.
   * [CHƯA IMPLEMENT] Thu hồi: trừ dần từ đơn tiếp theo / charge bank on file.

7. [CHƯA IMPLEMENT] Payout (Chuyển tiền về bank):
   * Seller chọn schedule: daily/weekly/monthly.
   * Hệ thống chuyển AvailableBalance → bank account. Tiền về trong 1-3 ngày.
   * Email thông báo: "Your payout of $XX has been initiated".

8. Seller Dashboard (Theo dõi tiền):
   * Seller xem real-time trên Seller Hub: Available / Pending / OnHold.
   * Transaction History: liệt kê từng giao dịch (sale, fee, refund, release, hold).
   * [CHƯA IMPLEMENT] Monthly financial statement PDF + CSV.
________________


PHẦN 5: LUỒNG MARKETING & KHUYẾN MÃI (MARKETING — CHUẨN EBAY 2024)
Công cụ giúp Seller kích cầu mua sắm. Truy cập qua Seller Hub → Marketing tab → Discounts.
eBay hỗ trợ 5 loại promotion: Coded Coupon, Sale Event (Markdown), Volume Pricing, Order Discount, Shipping Discount.
MVP scope: tập trung Coded Coupon (phase 1) + Sale Event (phase 2). Các loại còn lại defer.

1. Luồng Tạo & Quản lý Coded Coupon (Discounts → Coupon):
   * Yêu cầu: Shop phải IsVerified = True.
   * Bước 1 (Thông tin cơ bản):
      - Code: Mã giảm giá seller tự đặt, max **15 ký tự**, **unique per shop** (VD: SAVE20, REPEAT5).
      - Name: Tên campaign nội bộ (buyer KHÔNG thấy), dùng để seller quản lý.
   * Bước 2 (Loại giảm giá — DiscountType):
      - **PERCENTAGE**: Giảm theo % (VD: 20% off). BẮT BUỘC có MaxDiscountAmount cap (VD: 20% off, max giảm 100.000đ).
      - **FIXED_AMOUNT**: Giảm cố định (VD: giảm 50.000đ). Không cần cap.
      - [CHƯA IMPLEMENT] BOGO: Buy 1 Get 1 at X% off (phase 2+, cần cart multi-item).
   * Bước 3 (Điều kiện áp dụng):
      - MinOrderValue: Đơn tối thiểu để áp dụng (VD: đơn từ 200.000đ).
      - MaxDiscountAmount: Số tiền giảm tối đa (bắt buộc cho PERCENTAGE, tránh seller bị lỗ).
      - MaxBudget: Tổng ngân sách campaign. Khi UsedBudget >= MaxBudget → coupon tự tắt. (VD: budget 5 triệu, mỗi đơn giảm 50k → tối đa 100 đơn).
      - UsageLimit: Số lần dùng tối đa (tất cả buyers). Khi UsedCount >= UsageLimit → tắt.
      - PerBuyerLimit: Mỗi buyer dùng tối đa bao nhiêu lần (mặc định = 1). Tracking qua bảng VoucherUsages.
   * Bước 4 (Phạm vi áp dụng — Scope):
      - **SHOP**: Toàn bộ sản phẩm trong shop.
      - **PRODUCTS**: Chỉ sản phẩm cụ thể (ProductIds lưu JSON array Guid).
      - [CHƯA IMPLEMENT] BY_CATEGORY: Theo danh mục (cần CategoryIds).
   * Bước 5 (Hiển thị — Visibility):
      - **PRIVATE**: Mã không hiện trên eBay. Seller tự share qua kênh riêng (in kèm đơn, social media, email).
      - **PUBLIC**: Mã hiện trên search results, listing page, checkout. Terms & Conditions tự động hiển thị.
   * Bước 6 (Thời gian & Xuất bản):
      - ValidFrom / ValidTo: Thời gian hiệu lực.
      - Status lifecycle: DRAFT → ACTIVE (launch) → PAUSED (tạm dừng) → ENDED (hết hạn / manual end).
      - Nút: "💾 Lưu Nháp" (DRAFT) / "🚀 Khởi chạy" (ACTIVE).

2. Luồng Áp dụng Coupon (Buyer Checkout):
   * Buyer nhập mã coupon khi checkout → hệ thống validate:
      a. Code tồn tại + đúng ShopId.
      b. Status = ACTIVE.
      c. ValidFrom <= NOW <= ValidTo.
      d. UsedCount < UsageLimit.
      e. UsedBudget + discountAmount <= MaxBudget (nếu có).
      f. Buyer chưa dùng quá PerBuyerLimit (check bảng VoucherUsages).
      g. Order total >= MinOrderValue.
      h. Nếu Scope = PRODUCTS → check sản phẩm trong đơn có trong ProductIds.
   * Tính discount:
      - PERCENTAGE: discount = ItemSubtotal × Value / 100. NẾU discount > MaxDiscountAmount → discount = MaxDiscountAmount.
      - FIXED_AMOUNT: discount = Value (cố định).
      - discount KHÔNG BAO GIỜ vượt quá ItemSubtotal (không giảm âm).
   * Cập nhật:
      - Voucher: UsedCount += 1, UsedBudget += discount (ATOMIC UPDATE chống race condition).
      - Order: VoucherId = voucher.Id, DiscountAmount = discount.
      - TotalAmount = ItemSubtotal + ShippingFee - DiscountAmount.
      - Ghi VoucherUsage: { VoucherId, BuyerId, OrderId, DiscountAmount, UsedAt }.
   * LƯU Ý QUAN TRỌNG (Race Condition):
      - UsedCount và UsedBudget PHẢI update bằng Atomic SQL:
        UPDATE Vouchers SET UsedCount = UsedCount + 1, UsedBudget = UsedBudget + @discount
        WHERE Id = @id AND UsedCount < UsageLimit AND (MaxBudget IS NULL OR UsedBudget + @discount <= MaxBudget).
      - Nếu 0 rows affected → voucher đã hết → reject.

3. Quy tắc Phí Sàn khi có Coupon (QUAN TRỌNG — chuẩn eBay):
   * ⚠️ PlatformFee tính trên GIÁ GỐC (OriginalSubtotal), KHÔNG PHẢI giá sau discount.
   * Ví dụ: Item 1.500.000đ, coupon 5% off → buyer trả 1.425.000đ.
     PlatformFee = 5% × 1.500.000 = 75.000đ (tính trên giá gốc).
     Seller nhận: 1.425.000 - 75.000 = 1.350.000đ.
   * Lý do: Discount là seller tự nguyện offer → seller chịu toàn bộ chi phí giảm giá + phí sàn tính trên giá chưa giảm.
   * Order cần lưu: OriginalSubtotal (giá gốc trước discount, dùng để tính PlatformFee).

4. Quy tắc Hoàn tiền khi Order có Coupon:
   * Seller chỉ hoàn số tiền BUYER THỰC TRẢ (sau discount). VD: item 100k, coupon 10k → buyer trả 90k → refund 90k.
   * PlatformFee hoàn trên giá gốc (OriginalSubtotal): eBay hoàn phí sàn tính trên 100k gốc.
   * Coupon CÓ THỂ trả lại buyer nếu chưa hết hạn (tùy policy — MVP: không trả lại).
   * Partial refund: tính % trên số tiền buyer thực trả.

5. Stacking Rules (Chồng giảm giá):
   * 1 order chỉ dùng tối đa 1 coded coupon.
   * [Phase 2] Sale Event + Coded Coupon: CÓ THỂ stack (giảm giá markdown + thêm coupon). Hệ thống apply markdown trước → coupon sau.
   * Volume Pricing / Order Discount: KHÔNG stack với Coded Coupon. Hệ thống chọn discount tốt nhất cho buyer.

6. Quản lý Campaign (Dashboard):
   * Seller xem danh sách coupon: tabs Active / Scheduled / Paused / Ended.
   * Metrics mỗi coupon: UsedCount, UsedBudget, tổng đơn dùng coupon, sales lift.
   * [CHƯA IMPLEMENT] Reporting: base sales vs discounted sales, sales lift %.

7. [Phase 2] Sale Event (Markdown Manager):
   * Giảm giá TRỰC TIẾP trên giá listing (hiện giá gạch ngang + giá mới).
   * Entity SaleEvent: { Id, ShopId, Name, DiscountType, DiscountValue, StartDate, EndDate, Status, ItemSelectionRules }.
   * Khi Sale active: giá hiển thị = OriginalPrice × (1 - discount). Giá gốc gạch ngang.
   * PlatformFee: vẫn tính trên giá gốc (trước markdown). Seller chịu chi phí.
   * [CHƯA IMPLEMENT] Cần kết nối Product listing để hiển thị badge "SALE" + giá gạch.
________________


PHẦN 6: LUỒNG TƯƠNG TÁC & CHĂM SÓC KHÁCH HÀNG (CRM — CHUẨN EBAY 2024)
Giải quyết vấn đề hậu mãi và xây dựng uy tín. Feedback system theo chuẩn eBay (không dùng 1-5 sao).

1. Hệ thống Feedback (eBay-style — POSITIVE / NEUTRAL / NEGATIVE):
   * eBay thật KHÔNG dùng rating 1-5 sao. Feedback chỉ có 3 mức:
      - POSITIVE (😊): Trải nghiệm tốt.
      - NEUTRAL (😐): Bình thường.
      - NEGATIVE (😠): Trải nghiệm xấu.
   * Mỗi đơn hàng chỉ có đúng 1 feedback (1 Order = 1 Feedback, enforce bằng Unique Index trên OrderId).
   * Feedback KHÔNG THỂ sửa hoặc xóa sau khi gửi.

2. Luồng Buyer để lại Feedback:
   * Điều kiện bắt buộc:
      a. Order phải ở trạng thái DELIVERED hoặc COMPLETED.
      b. Buyer phải là chủ đơn hàng (order.BuyerId == buyerId).
      c. Chưa có feedback cho đơn này (1 order = 1 feedback).
      d. Trong thời hạn 60 ngày kể từ ngày giao hàng (DeliveredAt + 60 days).
   * Đầu vào: Rating (POSITIVE/NEUTRAL/NEGATIVE) + Comment (tùy chọn, max 500 ký tự).
   * Khi feedback được ghi nhận:
      a. Tạo bản ghi Feedback (OrderId, BuyerId, ShopId, Rating, Comment, CreatedAt).
      b. Cập nhật Shop feedback stats denormalized (xem mục 4).
      c. Tất cả trong 1 transaction (UnitOfWork.SaveChanges).
   * MVP Mock: TestBuyerController cung cấp endpoint POST /api/testbuyer/feedback. Dùng mock buyer (user role BUYER đầu tiên trong DB).

3. Luồng Seller Reply Feedback:
   * Seller chỉ được reply 1 LẦN DUY NHẤT (enforce ở Domain Entity: SetSellerReply() throw nếu đã reply).
   * Reply max 1000 ký tự, không được để trống.
   * Security: Chỉ seller của shop sở hữu feedback mới được reply (feedback.ShopId == shopId).
   * Sau khi reply: SellerReply + SellerRepliedAt được cập nhật. Cả feedback + reply hiển thị công khai.

4. Feedback Stats Denormalized (Performance Optimization):
   * Thống kê feedback được lưu DENORMALIZED trên bảng Shops để tránh COUNT mỗi lần load:
      - FeedbackScore = TotalPositive - TotalNegative (giống eBay Feedback Score).
      - TotalPositive, TotalNeutral, TotalNegative: Đếm theo loại.
      - PositivePercent = TotalPositive / Total × 100 (làm tròn 2 số thập phân).
   * Cập nhật: Mỗi khi có feedback mới → query lại COUNT từ bảng Feedbacks → ghi vào Shop.
   * LƯU Ý (Race Condition MVP): Hiện tại acceptable cho MVP. Phase 2 nên dùng atomic SQL: UPDATE Shops SET TotalPositive = TotalPositive + 1.

5. Seller Dashboard — Feedback Manager (/feedback):
   * Stats Header: FeedbackScore, PositivePercent (xanh ≥98%, vàng ≥95%, đỏ <95%), TotalP/N/Neg.
   * Filter Tabs: ALL / POSITIVE / NEUTRAL / NEGATIVE.
   * Feedback List: Paged (10/trang), hiển thị rating badge + buyer name (masked: "d***h") + đơn # + amount + comment + reply.
   * Inline Reply: Nút Reply mở textarea, submit gọi POST /api/feedback/{id}/reply.
   * API Endpoints (Seller — [Authorize]):
      - GET /api/feedback?page=&pageSize=&rating= → Danh sách paged.
      - GET /api/feedback/stats → Stats từ Shop denormalized.
      - POST /api/feedback/{feedbackId}/reply → Seller reply 1 lần.
      - GET /api/feedback/order/{orderId} → Xem feedback theo order.

6. Mock Buyer Feedback (Dev-only):
   * POST /api/testbuyer/feedback → Buyer mock để lại feedback.
   * GET /api/testbuyer/feedback/{orderId} → Check đã feedback chưa (trả { hasFeedback, feedback }).
   * Trang riêng: /buyer-test/feedback/{orderId} — UI chọn rating + nhập comment.
   * Nút "Leave Feedback" xuất hiện trong Mock Actions panel của OrderDetail khi DELIVERED/COMPLETED.

7. Database Schema (Feedback):
   * Bảng Feedbacks: Id (PK GUID), OrderId (FK UNIQUE → Orders), BuyerId (FK → Users), ShopId (FK → Shops), Rating (NVARCHAR 10), Comment (NVARCHAR 500), SellerReply (NVARCHAR 1000), SellerRepliedAt, CreatedAt.
   * Indexes: IX_Feedbacks_OrderId (UNIQUE), IX_Feedbacks_ShopId_CreatedAt (composite DESC).
   * FK Delete: Tất cả RESTRICT. Bảng Shops thêm 5 cột: FeedbackScore, TotalPositive, TotalNeutral, TotalNegative, PositivePercent.

8. Luồng Quản lý Khiếu nại (Dispute Management):
   * Khách mở khiếu nại → Đơn hàng → DISPUTE_OPENED. HoldForDispute(): tiền → OnHoldBalance (lock).
   * Seller nộp bằng chứng (ảnh đóng hàng). 3 business days deadline.
   * Seller đồng ý hoàn → ProcessRefund(). Admin xử Seller thua → ProcessRefund() + 1 Defect.

9. [CHƯA IMPLEMENT] CRM Phase 2:
   * Revision Feedback: Buyer sửa feedback trong 30 ngày nếu seller giải quyết.
   * Follow-up Feedback: Feedback bổ sung sau 60 ngày.
   * Feedback Reminder: Email buyer nhắc feedback 7 ngày sau delivery.
   * DSR (Detailed Seller Ratings): 4 tiêu chí phụ 1-5 sao.
   * Messaging System: Chat buyer-seller qua sàn.

________________


PHẦN 7: LUỒNG BÁO CÁO & ĐÁNH GIÁ HIỆU SUẤT (PERFORMANCE & ANALYTICS)
Công cụ thống kê và xếp hạng Seller.
1. Luồng Dashboard Thống kê (Sales Reporting):
* Giao diện: Tải siêu nhanh (dưới 1s). Hiển thị số lượng đơn hàng, doanh thu theo tuần/tháng.
* Kỹ thuật: Đọc dữ liệu từ bảng tổng hợp ShopAnalyticsDaily (đã được Job ban đêm tính toán sẵn), không query trực tiếp bảng Orders.
2. Luồng Ghi nhận Lỗi (Defect Tracking) & Đánh giá Cấp độ:
* Ghi nhận lỗi nếu: Seller tự hủy đơn do hết hàng, hoặc giao hàng trễ (Late Shipment).
* Ngày 20 hàng tháng, Job Evaluation tính tỉ lệ lỗi.
   * Lỗi > 2%: Hạ cấp BELOW_STANDARD, tăng phí sàn, giảm hạn mức đăng bài.
   * Bán tốt: Lên cấp TOP_RATED, giảm phí sàn.
________________


🚨 PHẦN 8: CHECKLIST KỸ THUẬT "SINH TỬ" CHO TEAM BACKEND (ROOT PRINCIPLES)
Để đảm bảo hệ thống chịu tải và không bị hack/lỗi dữ liệu:
1. Giao dịch Database (Transaction): Mọi thao tác đụng đến Tiền và Kho phải nằm trong BEGIN TRANSACTION ... COMMIT. Lỗi ở bất kỳ bước nào phải ROLLBACK.
2. Cập nhật Kho Nguyên tử (Atomic Update): Tuyệt đối không query số lượng ra rồi trừ bằng code. Phải dùng SQL: UPDATE ProductVariants SET Quantity = Quantity - 1 WHERE Id = X AND Quantity > 0.
3. Idempotency (Chống Click đúp): Ở nút "Thanh toán", nếu User lag bấm 5 lần, hệ thống chỉ sinh ra 1 đơn hàng duy nhất.
4. Hạn chế Spam (Rate Limiting & Security): API tạo sản phẩm/đơn hàng phải có Rate Limit (vd: 10 req/phút). Form nhạy cảm cần reCaptcha. Dữ liệu thanh toán giả lập phải có Auth Token.
5. Log lỗi chi tiết (Error Logging): Ghi log toàn bộ exception phân loại rõ Client Error (4xx - do user nhập sai) và Server Error (50x - kèm Transaction ID để truy vết). Bắt buộc log riêng và mã hóa các dữ liệu nhạy cảm.