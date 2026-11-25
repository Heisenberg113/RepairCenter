-- Development/demo data only. Run after the schema exists and the app has bootstrapped an active Admin.
-- Refuses to run when operational tables already contain data. Does not create accounts or passwords.
-- Contact details are synthetic placeholders; technician profiles are intentionally unlinked from login accounts.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.DeviceTypes)
        OR EXISTS (SELECT 1 FROM dbo.Customers)
        OR EXISTS (SELECT 1 FROM dbo.Devices)
        OR EXISTS (SELECT 1 FROM dbo.Technicians)
        OR EXISTS (SELECT 1 FROM dbo.TechnicianDeviceTypes)
        OR EXISTS (SELECT 1 FROM dbo.RepairServices)
        OR EXISTS (SELECT 1 FROM dbo.SpareParts)
        OR EXISTS (SELECT 1 FROM dbo.InventoryTransactions)
        OR EXISTS (SELECT 1 FROM dbo.RepairTickets)
        OR EXISTS (SELECT 1 FROM dbo.RepairTicketDetails)
        OR EXISTS (SELECT 1 FROM dbo.RepairTicketStatusHistories)
        OR EXISTS (SELECT 1 FROM dbo.RepairQuoteVersions)
        OR EXISTS (SELECT 1 FROM dbo.Invoices)
        OR EXISTS (SELECT 1 FROM dbo.InvoicePayments)
        OR EXISTS (SELECT 1 FROM dbo.Warranties)
        OR EXISTS (SELECT 1 FROM dbo.WarrantyClaims)
        OR EXISTS (SELECT 1 FROM dbo.CatalogChangeRequests)
    BEGIN
        THROW 51000, 'Demo seed only runs when all operational tables are empty.', 1;
    END;

    DECLARE @AdminUserId int;
    SELECT TOP (1) @AdminUserId = u.Id
    FROM dbo.Users AS u
    INNER JOIN dbo.UserRoles AS ur ON ur.UserId = u.Id
    INNER JOIN dbo.Roles AS r ON r.Id = ur.RoleId
    WHERE u.IsActive = 1 AND r.IsActive = 1 AND r.Name = N'Admin'
    ORDER BY u.Id;

    IF @AdminUserId IS NULL
    BEGIN
        THROW 51001, 'Configure BootstrapAdmin and start the app before running this seed.', 1;
    END;

    DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();
    DECLARE @TodayGmt7 date = CONVERT(date, DATEADD(HOUR, 7, @NowUtc));
    DECLARE @TodayStartUtc datetime2(7) = DATEADD(HOUR, -7, CONVERT(datetime2(7), @TodayGmt7));
    DECLARE @Time1Utc datetime2(7) = CASE WHEN DATEADD(MINUTE, -5, @NowUtc) < @TodayStartUtc THEN @TodayStartUtc ELSE DATEADD(MINUTE, -5, @NowUtc) END;
    DECLARE @Time2Utc datetime2(7) = CASE WHEN DATEADD(MINUTE, -30, @NowUtc) < @TodayStartUtc THEN @TodayStartUtc ELSE DATEADD(MINUTE, -30, @NowUtc) END;
    DECLARE @Time3Utc datetime2(7) = CASE WHEN DATEADD(HOUR, -1, @NowUtc) < @TodayStartUtc THEN @TodayStartUtc ELSE DATEADD(HOUR, -1, @NowUtc) END;
    DECLARE @Time4Utc datetime2(7) = CASE WHEN DATEADD(HOUR, -3, @NowUtc) < @TodayStartUtc THEN @TodayStartUtc ELSE DATEADD(HOUR, -3, @NowUtc) END;
    DECLARE @Time5Utc datetime2(7) = CASE WHEN DATEADD(HOUR, -2, @NowUtc) < @TodayStartUtc THEN @TodayStartUtc ELSE DATEADD(HOUR, -2, @NowUtc) END;

    DECLARE @LaptopTypeId int, @PhoneTypeId int, @TabletTypeId int;
    INSERT INTO dbo.DeviceTypes (Name) VALUES (N'Laptop'), (N'Điện thoại'), (N'Máy tính bảng');
    SELECT @LaptopTypeId = Id FROM dbo.DeviceTypes WHERE Name = N'Laptop';
    SELECT @PhoneTypeId = Id FROM dbo.DeviceTypes WHERE Name = N'Điện thoại';
    SELECT @TabletTypeId = Id FROM dbo.DeviceTypes WHERE Name = N'Máy tính bảng';

    DECLARE @Customer1Id int, @Customer2Id int, @Customer3Id int;
    INSERT INTO dbo.Customers (Name, Email, PhoneNumber, Address, CreatedAt)
    VALUES (N'Nguyễn An', N'an.nguyen@example.test', N'0000000001', N'Quận 1, TP. Hồ Chí Minh', DATEADD(DAY, -35, @NowUtc));
    SET @Customer1Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Customers (Name, Email, PhoneNumber, Address, CreatedAt)
    VALUES (N'Trần Bình', N'binh.tran@example.test', N'0000000002', N'Quận Bình Thạnh, TP. Hồ Chí Minh', DATEADD(DAY, -28, @NowUtc));
    SET @Customer2Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Customers (Name, Email, PhoneNumber, Address, CreatedAt)
    VALUES (N'Lê Minh', N'minh.le@example.test', N'0000000003', N'Thành phố Thủ Đức, TP. Hồ Chí Minh', DATEADD(DAY, -18, @NowUtc));
    SET @Customer3Id = CONVERT(int, SCOPE_IDENTITY());

    DECLARE @Device1Id int, @Device2Id int, @Device3Id int, @Device4Id int;
    INSERT INTO dbo.Devices (CustomerId, DeviceTypeId, Brand, Model, SerialNumber, Color, Description)
    VALUES (@Customer1Id, @LaptopTypeId, N'Dell', N'Latitude 5420', N'DEMO-LAT-0001', N'Đen', N'Máy văn phòng');
    SET @Device1Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Devices (CustomerId, DeviceTypeId, Brand, Model, SerialNumber, Color, Description)
    VALUES (@Customer2Id, @PhoneTypeId, N'Samsung', N'Galaxy S23', N'DEMO-GS23-0002', N'Kem', N'Điện thoại cá nhân');
    SET @Device2Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Devices (CustomerId, DeviceTypeId, Brand, Model, SerialNumber, Color, Description)
    VALUES (@Customer3Id, @TabletTypeId, N'Apple', N'iPad Air', N'DEMO-IPAD-0003', N'Xám', N'Máy tính bảng');
    SET @Device3Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Devices (CustomerId, DeviceTypeId, Brand, Model, SerialNumber, Color, Description)
    VALUES (@Customer1Id, @PhoneTypeId, N'Apple', N'iPhone 13', N'DEMO-IP13-0004', N'Xanh', N'Thiết bị phụ');
    SET @Device4Id = CONVERT(int, SCOPE_IDENTITY());

    DECLARE @Tech1Id int, @Tech2Id int, @Tech3Id int;
    INSERT INTO dbo.Technicians (UserId, EmployeeCode, FullName, PhoneNumber, Email, Specialty, IsActive)
    VALUES (NULL, N'DEMO-T001', N'Phạm Quốc Huy', N'0000000101', N'huy.pham@example.test', N'Laptop', 1);
    SET @Tech1Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Technicians (UserId, EmployeeCode, FullName, PhoneNumber, Email, Specialty, IsActive)
    VALUES (NULL, N'DEMO-T002', N'Võ Ngọc Lan', N'0000000102', N'lan.vo@example.test', N'Điện thoại', 1);
    SET @Tech2Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Technicians (UserId, EmployeeCode, FullName, PhoneNumber, Email, Specialty, IsActive)
    VALUES (NULL, N'DEMO-T003', N'Đặng Minh Khoa', N'0000000103', N'khoa.dang@example.test', N'Máy tính bảng', 1);
    SET @Tech3Id = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.TechnicianDeviceTypes (TechnicianId, DeviceTypeId)
    VALUES (@Tech1Id, @LaptopTypeId), (@Tech2Id, @PhoneTypeId), (@Tech3Id, @TabletTypeId);

    DECLARE @LaptopRepairId int, @LaptopDiagnosisId int, @PhoneScreenServiceId int;
    DECLARE @TabletRepairId int, @TabletPortServiceId int;
    INSERT INTO dbo.RepairServices (DeviceTypeId, Name, Description, UnitPrice, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@LaptopTypeId, N'Sửa bàn phím laptop', N'Thay hoặc sửa cụm bàn phím', 350000, 1, 90, 1, N'Bảo hành linh kiện theo điều kiện sử dụng.');
    SET @LaptopRepairId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairServices (DeviceTypeId, Name, Description, UnitPrice, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@LaptopTypeId, N'Chẩn đoán laptop', N'Kiểm tra lỗi phần cứng và phần mềm', 250000, 1, 0, 2, NULL);
    SET @LaptopDiagnosisId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairServices (DeviceTypeId, Name, Description, UnitPrice, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@PhoneTypeId, N'Thay màn hình điện thoại', N'Thay cụm màn hình tương thích', 1400000, 1, 60, 1, N'Tránh va đập và vào nước.');
    SET @PhoneScreenServiceId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairServices (DeviceTypeId, Name, Description, UnitPrice, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@TabletTypeId, N'Sửa pin máy tính bảng', N'Thay pin và kiểm tra mạch sạc', 950000, 1, 90, 1, N'Bảo hành pin theo điều kiện sử dụng.');
    SET @TabletRepairId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairServices (DeviceTypeId, Name, Description, UnitPrice, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@TabletTypeId, N'Vệ sinh cổng sạc máy tính bảng', N'Vệ sinh và kiểm tra cổng sạc', 450000, 1, 30, 1, N'Không bao gồm hư hỏng do chất lỏng.');
    SET @TabletPortServiceId = CONVERT(int, SCOPE_IDENTITY());

    DECLARE @SsdPartId int, @PhoneScreenPartId int, @TabletBatteryPartId int, @ChargerPartId int;
    INSERT INTO dbo.SpareParts (DeviceTypeId, PartNumber, Name, Description, UnitCost, SellingPrice, QuantityInStock, ReorderLevel, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@LaptopTypeId, N'DEMO-SSD-512', N'SSD NVMe 512 GB', N'Ổ cứng thay thế cho laptop', 900000, 1350000, 8, 3, 1, 180, 1, N'Bảo hành theo chính sách nhà cung cấp.');
    SET @SsdPartId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.SpareParts (DeviceTypeId, PartNumber, Name, Description, UnitCost, SellingPrice, QuantityInStock, ReorderLevel, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@PhoneTypeId, N'DEMO-SCR-S23', N'Cụm màn hình Galaxy S23', N'Màn hình thay thế', 720000, 1100000, 3, 2, 1, 60, 1, N'Tránh va đập và vào nước.');
    SET @PhoneScreenPartId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.SpareParts (DeviceTypeId, PartNumber, Name, Description, UnitCost, SellingPrice, QuantityInStock, ReorderLevel, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@TabletTypeId, N'DEMO-BAT-IPAD', N'Pin iPad Air', N'Pin thay thế cho máy tính bảng', 150000, 250000, 4, 2, 1, 90, 1, N'Bảo hành pin theo điều kiện sử dụng.');
    SET @TabletBatteryPartId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.SpareParts (DeviceTypeId, PartNumber, Name, Description, UnitCost, SellingPrice, QuantityInStock, ReorderLevel, IsActive, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms)
    VALUES (@PhoneTypeId, N'DEMO-CHG-USBC', N'Củ sạc USB-C', N'Củ sạc mẫu dùng cho thiết bị tương thích', 180000, 300000, 8, 3, 1, 30, 1, N'Bảo hành lỗi kỹ thuật.');
    SET @ChargerPartId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.InventoryTransactions (SparePartId, TransactionType, Quantity, UnitCost, ReferenceNumber, TransactionDate, Note)
    VALUES
        (@SsdPartId, N'StockIn', 10, 900000, N'DEMO-PO-001', DATEADD(DAY, -10, @NowUtc), N'Nhập kho mẫu'),
        (@SsdPartId, N'StockOut', 2, 900000, N'DEMO-USE-001', DATEADD(DAY, -4, @NowUtc), N'Xuất dùng cho phiếu sửa chữa'),
        (@PhoneScreenPartId, N'StockIn', 5, 720000, N'DEMO-PO-002', DATEADD(DAY, -8, @NowUtc), N'Nhập kho mẫu'),
        (@PhoneScreenPartId, N'StockOut', 2, 720000, N'DEMO-USE-002', DATEADD(DAY, -3, @NowUtc), N'Xuất dùng cho phiếu sửa chữa'),
        (@TabletBatteryPartId, N'StockIn', 5, 150000, N'DEMO-PO-003', DATEADD(DAY, -6, @NowUtc), N'Nhập kho mẫu'),
        (@TabletBatteryPartId, N'StockOut', 1, 150000, N'DEMO-USE-003', DATEADD(DAY, -2, @NowUtc), N'Xuất dùng cho phiếu sửa chữa'),
        (@ChargerPartId, N'StockIn', 10, 180000, N'DEMO-PO-004', DATEADD(DAY, -5, @NowUtc), N'Nhập kho mẫu'),
        (@ChargerPartId, N'StockOut', 2, 180000, N'DEMO-USE-004', DATEADD(DAY, -1, @NowUtc), N'Xuất dùng cho phiếu sửa chữa');

    DECLARE @Ticket1Id int, @Ticket2Id int, @Ticket3Id int, @Ticket4Id int, @Ticket5Id int, @Ticket6Id int;
    INSERT INTO dbo.RepairTickets
        (TicketNumber, CustomerId, DeviceId, TechnicianId, ParentRepairTicketId, IsWarrantyRepair, Status, IssueDescription, Diagnosis, EstimatedCost, ActualCost, ReceivedAt, ExpectedCompletionAt, CompletedAt, DeliveredAt, CustomerNotes, InternalNotes)
    VALUES
        (N'RC-DEMO-2026-0001', @Customer1Id, @Device1Id, @Tech1Id, NULL, 0, 7, N'Bàn phím một số phím không hoạt động, máy chạy chậm.', N'Thay bàn phím và nâng cấp ổ lưu trữ.', 1700000, 1700000, DATEADD(DAY, -4, @NowUtc), NULL, DATEADD(HOUR, -72, @NowUtc), DATEADD(HOUR, -70, @NowUtc), N'Liên hệ trước khi giao máy.', N'Dữ liệu demo'),
        (N'RC-DEMO-2026-0002', @Customer2Id, @Device2Id, @Tech2Id, NULL, 0, 7, N'Màn hình nứt sau va chạm, cảm ứng chập chờn.', N'Thay cụm màn hình.', 2500000, 2500000, DATEADD(DAY, -3, @NowUtc), NULL, DATEADD(HOUR, -62, @NowUtc), DATEADD(HOUR, -60, @NowUtc), NULL, N'Dữ liệu demo'),
        (N'RC-DEMO-2026-0003', @Customer3Id, @Device3Id, @Tech3Id, NULL, 0, 6, N'Thời lượng pin giảm nhanh, máy tự tắt.', N'Thay pin và kiểm tra chu kỳ sạc.', 1200000, 1200000, DATEADD(DAY, -2, @NowUtc), NULL, DATEADD(HOUR, -40, @NowUtc), NULL, NULL, N'Dữ liệu demo'),
        (N'RC-DEMO-2026-0004', @Customer1Id, @Device1Id, @Tech1Id, NULL, 0, 6, N'Máy nóng và quạt phát tiếng ồn.', N'Vệ sinh hệ thống tản nhiệt, hoàn tất chẩn đoán.', 250000, 250000, DATEADD(DAY, -21, @NowUtc), NULL, DATEADD(DAY, -20, @NowUtc), NULL, NULL, N'Dữ liệu demo'),
        (N'RC-DEMO-2026-0005', @Customer3Id, @Device3Id, @Tech3Id, NULL, 0, 4, N'Cổng sạc tiếp xúc không ổn định.', N'Đã được khách duyệt báo giá, đang sửa.', 450000, NULL, DATEADD(HOUR, -10, @NowUtc), DATEADD(DAY, 1, @NowUtc), NULL, NULL, NULL, N'Dữ liệu demo'),
        (N'RC-DEMO-2026-0006', @Customer1Id, @Device4Id, NULL, NULL, 0, 1, N'Pin điện thoại chai, cần kiểm tra.', NULL, NULL, NULL, DATEADD(HOUR, -2, @NowUtc), DATEADD(DAY, 2, @NowUtc), NULL, NULL, NULL, N'Phiếu mới tiếp nhận');
    SELECT @Ticket1Id = Id FROM dbo.RepairTickets WHERE TicketNumber = N'RC-DEMO-2026-0001';
    SELECT @Ticket2Id = Id FROM dbo.RepairTickets WHERE TicketNumber = N'RC-DEMO-2026-0002';
    SELECT @Ticket3Id = Id FROM dbo.RepairTickets WHERE TicketNumber = N'RC-DEMO-2026-0003';
    SELECT @Ticket4Id = Id FROM dbo.RepairTickets WHERE TicketNumber = N'RC-DEMO-2026-0004';
    SELECT @Ticket5Id = Id FROM dbo.RepairTickets WHERE TicketNumber = N'RC-DEMO-2026-0005';
    SELECT @Ticket6Id = Id FROM dbo.RepairTickets WHERE TicketNumber = N'RC-DEMO-2026-0006';

    DECLARE @Ticket1ServiceDetailId int, @Ticket1PartDetailId int;
    DECLARE @Ticket2ServiceDetailId int, @Ticket2PartDetailId int;
    DECLARE @Ticket3ServiceDetailId int, @Ticket3PartDetailId int;
    DECLARE @Ticket4ServiceDetailId int, @Ticket5ServiceDetailId int;
    DECLARE @Ticket1WarrantyStart datetime2(7) =
        CONVERT(datetime2(7), CONVERT(date, DATEADD(HOUR, 7, DATEADD(HOUR, -70, @NowUtc))));
    DECLARE @Ticket2WarrantyStart datetime2(7) =
        CONVERT(datetime2(7), CONVERT(date, DATEADD(HOUR, 7, DATEADD(HOUR, -60, @NowUtc))));
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket1Id, @LaptopRepairId, NULL, N'Sửa bàn phím laptop', 1, 350000, 0, 0, 350000, 90, 1, N'Bảo hành linh kiện theo điều kiện sử dụng.', @Ticket1WarrantyStart, DATEADD(DAY, 90, @Ticket1WarrantyStart));
    SET @Ticket1ServiceDetailId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket1Id, NULL, @SsdPartId, N'SSD NVMe 512 GB', 1, 1350000, 0, 0, 1350000, 180, 1, N'Bảo hành theo chính sách nhà cung cấp.', @Ticket1WarrantyStart, DATEADD(DAY, 180, @Ticket1WarrantyStart));
    SET @Ticket1PartDetailId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket2Id, @PhoneScreenServiceId, NULL, N'Thay màn hình điện thoại', 1, 1400000, 0, 0, 1400000, 60, 1, N'Tránh va đập và vào nước.', @Ticket2WarrantyStart, DATEADD(DAY, 60, @Ticket2WarrantyStart));
    SET @Ticket2ServiceDetailId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket2Id, NULL, @PhoneScreenPartId, N'Cụm màn hình Galaxy S23', 1, 1100000, 0, 0, 1100000, 60, 1, N'Tránh va đập và vào nước.', @Ticket2WarrantyStart, DATEADD(DAY, 60, @Ticket2WarrantyStart));
    SET @Ticket2PartDetailId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket3Id, @TabletRepairId, NULL, N'Sửa pin máy tính bảng', 1, 950000, 0, 0, 950000, 90, 1, N'Bảo hành pin theo điều kiện sử dụng.', NULL, NULL);
    SET @Ticket3ServiceDetailId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket3Id, NULL, @TabletBatteryPartId, N'Pin iPad Air', 1, 250000, 0, 0, 250000, 90, 1, N'Bảo hành pin theo điều kiện sử dụng.', NULL, NULL);
    SET @Ticket3PartDetailId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket4Id, @LaptopDiagnosisId, NULL, N'Chẩn đoán laptop', 1, 250000, 0, 0, 250000, 0, 2, NULL, NULL, NULL);
    SET @Ticket4ServiceDetailId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.RepairTicketDetails
        (RepairTicketId, RepairServiceId, SparePartId, Description, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalAmount, WarrantyDuration, WarrantyDurationUnit, WarrantyTerms, WarrantyStartDate, WarrantyEndDate)
    VALUES (@Ticket5Id, @TabletPortServiceId, NULL, N'Tablet port cleaning', 1.00, 450000.00, 0.0000, 0.00, 450000.00, 30, 1, N'Không bao gồm hư hỏng do chất lỏng.', NULL, NULL);
    SET @Ticket5ServiceDetailId = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.RepairTicketStatusHistories (RepairTicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Note)
    VALUES
        (@Ticket1Id, 1, 2, @AdminUserId, DATEADD(HOUR, -95, @NowUtc), N'Tiếp nhận và kiểm tra'),
        (@Ticket1Id, 2, 4, @AdminUserId, DATEADD(HOUR, -90, @NowUtc), N'Bắt đầu sửa chữa'),
        (@Ticket1Id, 4, 6, @AdminUserId, DATEADD(HOUR, -72, @NowUtc), N'Đã hoàn tất'),
        (@Ticket1Id, 6, 7, @AdminUserId, DATEADD(HOUR, -70, @NowUtc), N'Đã bàn giao'),
        (@Ticket2Id, 1, 2, @AdminUserId, DATEADD(HOUR, -71, @NowUtc), N'Tiếp nhận và kiểm tra'),
        (@Ticket2Id, 2, 3, @AdminUserId, DATEADD(HOUR, -68, @NowUtc), N'Đã gửi báo giá'),
        (@Ticket2Id, 3, 4, @AdminUserId, DATEADD(HOUR, -66, @NowUtc), N'Khách đồng ý báo giá'),
        (@Ticket2Id, 4, 6, @AdminUserId, DATEADD(HOUR, -62, @NowUtc), N'Đã hoàn tất'),
        (@Ticket2Id, 6, 7, @AdminUserId, DATEADD(HOUR, -60, @NowUtc), N'Đã bàn giao'),
        (@Ticket3Id, 1, 2, @AdminUserId, DATEADD(HOUR, -47, @NowUtc), N'Tiếp nhận và kiểm tra'),
        (@Ticket3Id, 2, 4, @AdminUserId, DATEADD(HOUR, -44, @NowUtc), N'Bắt đầu sửa chữa'),
        (@Ticket3Id, 4, 6, @AdminUserId, DATEADD(HOUR, -40, @NowUtc), N'Đã hoàn tất'),
        (@Ticket4Id, 1, 2, @AdminUserId, DATEADD(HOUR, -503, @NowUtc), N'Tiếp nhận và kiểm tra'),
        (@Ticket4Id, 2, 4, @AdminUserId, DATEADD(HOUR, -500, @NowUtc), N'Bắt đầu chẩn đoán'),
        (@Ticket4Id, 4, 6, @AdminUserId, DATEADD(HOUR, -490, @NowUtc), N'Đã hoàn tất'),
        (@Ticket5Id, 1, 2, @AdminUserId, DATEADD(HOUR, -9, @NowUtc), N'Tiếp nhận và kiểm tra'),
        (@Ticket5Id, 2, 3, @AdminUserId, DATEADD(HOUR, -8, @NowUtc), N'Đã gửi báo giá'),
        (@Ticket5Id, 3, 4, @AdminUserId, DATEADD(HOUR, -7, @NowUtc), N'Khách đồng ý báo giá');

    DECLARE @SnapshotJson nvarchar(20000) =
        N'[{"detailId":' + CONVERT(nvarchar(20), @Ticket5ServiceDetailId) +
        N',"itemType":"Service","description":"Tablet port cleaning","quantity":1.00,"unitPrice":450000.00,"discountPercent":0.0000,"discountAmount":0.00,"totalAmount":450000.00}]';
    DECLARE @SnapshotHash varchar(64) = UPPER(CONVERT(varchar(64),
        HASHBYTES('SHA2_256', CONVERT(varbinary(max), CONVERT(varchar(max), @SnapshotJson))), 2));
    INSERT INTO dbo.RepairQuoteVersions
        (RepairTicketId, VersionNumber, SnapshotJson, SnapshotHash, TotalAmount, CreatedByUserId, CreatedAt, Response, RespondedByUserId, RespondedAt, ResponseChannel, ResponseNote)
    VALUES (@Ticket5Id, 1, @SnapshotJson, @SnapshotHash, 450000, @AdminUserId, DATEADD(HOUR, -8, @NowUtc), N'Accepted', @AdminUserId, DATEADD(HOUR, -7, @NowUtc), N'Điện thoại', N'Khách đồng ý sửa chữa.');

    DECLARE @Invoice1Id int, @Invoice2Id int, @Invoice3Id int, @Invoice4Id int;
    INSERT INTO dbo.Invoices
        (InvoiceNumber, RepairTicketId, IssuedAt, DueAt, Subtotal, DiscountPercent, DiscountAmount, TaxPercent, TaxAmount, TotalAmount, AmountPaid, PaymentStatus, PaidAt, Notes)
    VALUES (N'INV-DEMO-2026-0001', @Ticket1Id, DATEADD(HOUR, -70, @NowUtc), CONVERT(datetime2(7), DATEADD(DAY, 30, @TodayGmt7)), 1700000, 0, 0, 0, 0, 1700000, 1700000, 3, @Time1Utc, N'Hóa đơn mẫu đã thanh toán.');
    SET @Invoice1Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Invoices
        (InvoiceNumber, RepairTicketId, IssuedAt, DueAt, Subtotal, DiscountPercent, DiscountAmount, TaxPercent, TaxAmount, TotalAmount, AmountPaid, PaymentStatus, PaidAt, Notes)
    VALUES (N'INV-DEMO-2026-0002', @Ticket2Id, DATEADD(HOUR, -60, @NowUtc), CONVERT(datetime2(7), DATEADD(DAY, 30, @TodayGmt7)), 2500000, 0, 0, 0, 0, 2500000, 1500000, 2, NULL, N'Hóa đơn mẫu thanh toán một phần.');
    SET @Invoice2Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Invoices
        (InvoiceNumber, RepairTicketId, IssuedAt, DueAt, Subtotal, DiscountPercent, DiscountAmount, TaxPercent, TaxAmount, TotalAmount, AmountPaid, PaymentStatus, PaidAt, Notes)
    VALUES (N'INV-DEMO-2026-0003', @Ticket3Id, DATEADD(HOUR, -40, @NowUtc), CONVERT(datetime2(7), DATEADD(DAY, 30, @TodayGmt7)), 1200000, 0, 0, 0, 0, 1200000, 0, 4, NULL, N'Hóa đơn mẫu đã hoàn tiền toàn bộ.');
    SET @Invoice3Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Invoices
        (InvoiceNumber, RepairTicketId, IssuedAt, DueAt, Subtotal, DiscountPercent, DiscountAmount, TaxPercent, TaxAmount, TotalAmount, AmountPaid, PaymentStatus, PaidAt, Notes)
    VALUES (N'INV-DEMO-2026-0004', @Ticket4Id, DATEADD(DAY, -20, @NowUtc), CONVERT(datetime2(7), DATEADD(DAY, -5, @TodayGmt7)), 250000, 0, 0, 0, 0, 250000, 0, 5, NULL, N'Hóa đơn mẫu quá hạn.');
    SET @Invoice4Id = CONVERT(int, SCOPE_IDENTITY());

    DECLARE @RefundedPaymentId int;
    INSERT INTO dbo.InvoicePayments
        (InvoiceId, Amount, Type, ProcessedByUserId, RefundOfPaymentId, PaymentMethod, ReferenceNumber, PaidAt, Notes)
    VALUES (@Invoice1Id, 1700000, N'Payment', @AdminUserId, NULL, N'Tiền mặt', N'DEMO-PAY-0001', @Time1Utc, N'Thanh toán đủ hóa đơn.');
    INSERT INTO dbo.InvoicePayments
        (InvoiceId, Amount, Type, ProcessedByUserId, RefundOfPaymentId, PaymentMethod, ReferenceNumber, PaidAt, Notes)
    VALUES (@Invoice2Id, 1000000, N'Payment', @AdminUserId, NULL, N'Chuyển khoản', N'DEMO-PAY-0002', @Time4Utc, N'Thanh toán đợt đầu.');
    INSERT INTO dbo.InvoicePayments
        (InvoiceId, Amount, Type, ProcessedByUserId, RefundOfPaymentId, PaymentMethod, ReferenceNumber, PaidAt, Notes)
    VALUES (@Invoice2Id, 500000, N'Payment', @AdminUserId, NULL, N'Tiền mặt', N'DEMO-PAY-0003', @Time3Utc, N'Thanh toán bổ sung.');
    INSERT INTO dbo.InvoicePayments
        (InvoiceId, Amount, Type, ProcessedByUserId, RefundOfPaymentId, PaymentMethod, ReferenceNumber, PaidAt, Notes)
    VALUES (@Invoice3Id, 1200000, N'Payment', @AdminUserId, NULL, N'Chuyển khoản', N'DEMO-PAY-0004', @Time4Utc, N'Thanh toán trước khi hoàn tiền.');
    SET @RefundedPaymentId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.InvoicePayments
        (InvoiceId, Amount, Type, ProcessedByUserId, RefundOfPaymentId, PaymentMethod, ReferenceNumber, PaidAt, Notes)
    VALUES (@Invoice3Id, 1200000, N'Refund', @AdminUserId, @RefundedPaymentId, N'Chuyển khoản', N'DEMO-REF-0001', @Time5Utc, N'Hoàn tiền toàn bộ giao dịch mẫu.');

    DECLARE @Warranty1Id int, @Warranty2Id int;
    INSERT INTO dbo.Warranties (WarrantyCode, RepairTicketId, StartDate, EndDate, Status, Terms, VoidedAt, VoidedByUserId, VoidReason)
    VALUES (N'WAR-DEMO-0001', @Ticket1Id, @Ticket1WarrantyStart, DATEADD(DAY, 180, @Ticket1WarrantyStart), 1, N'Bảo hành theo chính sách từng hạng mục.', NULL, NULL, NULL);
    SET @Warranty1Id = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO dbo.Warranties (WarrantyCode, RepairTicketId, StartDate, EndDate, Status, Terms, VoidedAt, VoidedByUserId, VoidReason)
    VALUES (N'WAR-DEMO-0002', @Ticket2Id, @Ticket2WarrantyStart, DATEADD(DAY, 60, @Ticket2WarrantyStart), 1, N'Bảo hành theo chính sách từng hạng mục.', NULL, NULL, NULL);
    SET @Warranty2Id = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.WarrantyClaims
        (WarrantyId, RepairTicketDetailId, RequestedByUserId, ResolvedByUserId, AssignedTechnicianId, AssignedAt, GeneratedRepairTicketId, Description, ResolutionNote, Status, SubmittedAt, ResolvedAt)
    VALUES
        (@Warranty1Id, @Ticket1PartDetailId, @AdminUserId, NULL, @Tech1Id, @Time2Utc, NULL, N'Ổ cứng phát tiếng ồn bất thường, đề nghị kiểm tra bảo hành.', NULL, 5, DATEADD(DAY, -1, @NowUtc), NULL),
        (@Warranty2Id, @Ticket2ServiceDetailId, @AdminUserId, @AdminUserId, NULL, NULL, NULL, N'Màn hình thỉnh thoảng không nhận cảm ứng.', N'Kiểm tra cho thấy thiết bị bị va đập ngoài điều kiện bảo hành.', 3, DATEADD(DAY, -2, @NowUtc), DATEADD(DAY, -1, @NowUtc));

    COMMIT TRANSACTION;
    PRINT 'RepairCenter demo seed completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
