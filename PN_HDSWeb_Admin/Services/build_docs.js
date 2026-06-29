const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  AlignmentType, LevelFormat, HeadingLevel, BorderStyle, WidthType, ShadingType,
  VerticalAlign, PageNumber, PageBreak, Header, Footer,
  TableOfContents
} = require('docx');
const fs = require('fs');

// Colors
const COLOR_PRIMARY = "1565C0";
const COLOR_HEADER_BG = "1565C0";
const COLOR_HEADER_TEXT = "FFFFFF";
const COLOR_ROW_ALT = "EBF3FB";
const COLOR_CODE_BG = "F0F4F8";
const COLOR_BORDER = "BBDEFB";
const COLOR_GET = "1B5E20";
const COLOR_POST = "0D47A1";
const COLOR_PUT = "E65100";
const COLOR_TAG_GET_BG = "E8F5E9";
const COLOR_TAG_POST_BG = "E3F2FD";
const COLOR_TAG_PUT_BG = "FFF3E0";

const borderDef = { style: BorderStyle.SINGLE, size: 1, color: COLOR_BORDER };
const borders = { top: borderDef, bottom: borderDef, left: borderDef, right: borderDef };
const noBorder = { style: BorderStyle.NONE, size: 0, color: "FFFFFF" };
const noBorders = { top: noBorder, bottom: noBorder, left: noBorder, right: noBorder };

const PAGE_W = 11906;
const MARGIN = 1134;
const CONTENT_W = PAGE_W - MARGIN * 2;

function heading1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 360, after: 120 },
    border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: COLOR_PRIMARY, space: 6 } },
    children: [new TextRun({ text, bold: true, size: 28, color: COLOR_PRIMARY, font: "Arial" })]
  });
}

function heading2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 280, after: 100 },
    children: [new TextRun({ text, bold: true, size: 24, color: COLOR_PRIMARY, font: "Arial" })]
  });
}

function para(text, opts = {}) {
  return new Paragraph({
    spacing: { after: 100 },
    children: [new TextRun({ text, size: 20, font: "Arial", ...opts })]
  });
}

function paraRuns(runs) {
  return new Paragraph({
    spacing: { after: 100 },
    children: runs
  });
}

function codeBlock(lines) {
  return new Table({
    width: { size: CONTENT_W, type: WidthType.DXA },
    columnWidths: [CONTENT_W],
    rows: [
      new TableRow({
        children: [
          new TableCell({
            borders: {
              top: { style: BorderStyle.SINGLE, size: 1, color: "90CAF9" },
              bottom: { style: BorderStyle.SINGLE, size: 1, color: "90CAF9" },
              left: { style: BorderStyle.THICK, size: 12, color: COLOR_PRIMARY },
              right: { style: BorderStyle.SINGLE, size: 1, color: "90CAF9" },
            },
            shading: { fill: COLOR_CODE_BG, type: ShadingType.CLEAR },
            margins: { top: 100, bottom: 100, left: 160, right: 160 },
            width: { size: CONTENT_W, type: WidthType.DXA },
            children: lines.map(line => new Paragraph({
              spacing: { after: 0, line: 240 },
              children: [new TextRun({ text: line, font: "Courier New", size: 18, color: "1A237E" })]
            }))
          })
        ]
      })
    ]
  });
}

function spacer(pts = 80) {
  return new Paragraph({ spacing: { after: pts }, children: [] });
}

function methodBadge(method, color, bgColor) {
  return new TableCell({
    borders: noBorders,
    shading: { fill: bgColor, type: ShadingType.CLEAR },
    margins: { top: 60, bottom: 60, left: 120, right: 120 },
    width: { size: 1200, type: WidthType.DXA },
    verticalAlign: VerticalAlign.CENTER,
    children: [new Paragraph({
      alignment: AlignmentType.CENTER,
      children: [new TextRun({ text: method, bold: true, size: 20, color, font: "Arial" })]
    })]
  });
}

function endpointRow(method, url) {
  const isGet = method === "GET";
  const isPut = method === "PUT";
  const methodColor = isGet ? COLOR_GET : isPut ? COLOR_PUT : COLOR_POST;
  const methodBg = isGet ? COLOR_TAG_GET_BG : isPut ? COLOR_TAG_PUT_BG : COLOR_TAG_POST_BG;

  return new Table({
    width: { size: CONTENT_W, type: WidthType.DXA },
    columnWidths: [1200, CONTENT_W - 1200],
    rows: [new TableRow({
      children: [
        methodBadge(method, methodColor, methodBg),
        new TableCell({
          borders: {
            top: borderDef, bottom: borderDef, left: borderDef, right: borderDef,
          },
          shading: { fill: "FAFAFA", type: ShadingType.CLEAR },
          margins: { top: 60, bottom: 60, left: 160, right: 120 },
          width: { size: CONTENT_W - 1200, type: WidthType.DXA },
          verticalAlign: VerticalAlign.CENTER,
          children: [new Paragraph({
            children: [new TextRun({ text: url, font: "Courier New", size: 20, bold: true, color: "0D47A1" })]
          })]
        })
      ]
    })]
  });
}

function makeTableHeader(cols, widths) {
  return new TableRow({
    tableHeader: true,
    children: cols.map((col, i) => new TableCell({
      borders,
      shading: { fill: COLOR_HEADER_BG, type: ShadingType.CLEAR },
      margins: { top: 80, bottom: 80, left: 120, right: 120 },
      width: { size: widths[i], type: WidthType.DXA },
      verticalAlign: VerticalAlign.CENTER,
      children: [new Paragraph({
        children: [new TextRun({ text: col, bold: true, size: 20, color: COLOR_HEADER_TEXT, font: "Arial" })]
      })]
    }))
  });
}

function makeTableRow(cells, widths, isAlt = false) {
  return new TableRow({
    children: cells.map((cell, i) => new TableCell({
      borders,
      shading: { fill: isAlt ? COLOR_ROW_ALT : "FFFFFF", type: ShadingType.CLEAR },
      margins: { top: 60, bottom: 60, left: 120, right: 120 },
      width: { size: widths[i], type: WidthType.DXA },
      children: [new Paragraph({
        children: [new TextRun({ text: cell, size: 20, font: "Arial" })]
      })]
    }))
  });
}

function infoBox(label, value, valueCode = false) {
  const w1 = 2400, w2 = CONTENT_W - 2400;
  return new Table({
    width: { size: CONTENT_W, type: WidthType.DXA },
    columnWidths: [w1, w2],
    rows: [new TableRow({
      children: [
        new TableCell({
          borders,
          shading: { fill: "EEF4FB", type: ShadingType.CLEAR },
          margins: { top: 80, bottom: 80, left: 120, right: 120 },
          width: { size: w1, type: WidthType.DXA },
          children: [new Paragraph({
            children: [new TextRun({ text: label, bold: true, size: 20, font: "Arial" })]
          })]
        }),
        new TableCell({
          borders,
          shading: { fill: "FFFFFF", type: ShadingType.CLEAR },
          margins: { top: 80, bottom: 80, left: 120, right: 120 },
          width: { size: w2, type: WidthType.DXA },
          children: [new Paragraph({
            children: [new TextRun({ text: value, size: 20, font: valueCode ? "Courier New" : "Arial", color: valueCode ? "1A237E" : "000000" })]
          })]
        })
      ]
    })]
  });
}

function bullet(text) {
  return new Paragraph({
    numbering: { reference: "bullets", level: 0 },
    spacing: { after: 60 },
    children: [new TextRun({ text, size: 20, font: "Arial" })]
  });
}

// ======================== DOCUMENT ========================
const doc = new Document({
  styles: {
    default: { document: { run: { font: "Arial", size: 20 } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 28, bold: true, font: "Arial", color: COLOR_PRIMARY },
        paragraph: { spacing: { before: 360, after: 120 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 24, bold: true, font: "Arial", color: COLOR_PRIMARY },
        paragraph: { spacing: { before: 280, after: 100 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 22, bold: true, font: "Arial" },
        paragraph: { spacing: { before: 200, after: 80 }, outlineLevel: 2 } },
    ]
  },
  numbering: {
    config: [
      { reference: "bullets",
        levels: [{ level: 0, format: LevelFormat.BULLET, text: "\u2022", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
    ]
  },
  sections: [{
    properties: {
      page: {
        size: { width: PAGE_W, height: 16838 },
        margin: { top: MARGIN, right: MARGIN, bottom: MARGIN, left: MARGIN }
      }
    },
    headers: {
      default: new Header({
        children: [
          new Paragraph({
            border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: COLOR_PRIMARY, space: 4 } },
            children: [
              new TextRun({ text: "TÀI LIỆU ĐẶC TẢ API  |  Camera Door & Điểm Danh Kho  |  ", size: 18, font: "Arial", color: "666666" }),
              new TextRun({ text: "TITKUL", size: 18, bold: true, font: "Arial", color: COLOR_PRIMARY }),
            ]
          })
        ]
      })
    },
    footers: {
      default: new Footer({
        children: [
          new Paragraph({
            border: { top: { style: BorderStyle.SINGLE, size: 4, color: COLOR_PRIMARY, space: 4 } },
            alignment: AlignmentType.RIGHT,
            children: [
              new TextRun({ text: "Trang ", size: 18, font: "Arial", color: "666666" }),
              new TextRun({ children: [PageNumber.CURRENT], size: 18, font: "Arial", color: "666666" }),
              new TextRun({ text: " / ", size: 18, font: "Arial", color: "666666" }),
              new TextRun({ children: [PageNumber.TOTAL_PAGES], size: 18, font: "Arial", color: "666666" }),
            ]
          })
        ]
      })
    },
    children: [
      // ===== COVER =====
      spacer(800),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 60 },
        children: [new TextRun({ text: "TÀI LIỆU ĐẶC TẢ API", size: 52, bold: true, font: "Arial", color: COLOR_PRIMARY })]
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 60 },
        children: [new TextRun({ text: "Camera Door & Điểm Danh Ra/Vào Kho", size: 32, font: "Arial", color: "37474F" })]
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 200 },
        border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: COLOR_PRIMARY, space: 8 } },
        children: []
      }),

      ...([
        ["Domain", "https://facekhothuoc.nhidong.org.vn/"],
        ["Security Key", "b9X#4qLm2@Np7VzR8!sKd1Yw6TcH"],
        ["Base Path", "/api/camera-door"],
        ["Phiên bản", "1.0.0"],
        ["Cập nhật", "06/2026"],
      ].map(([k, v]) => infoBox(k, v, ["Security Key", "Base Path"].includes(k)))),

      spacer(200),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 0 },
        children: [new TextRun({ text: "Công Ty Cổ Phần TITKUL", size: 20, bold: true, font: "Arial", color: "666666" })]
      }),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== TOC =====
      new Paragraph({
        heading: HeadingLevel.HEADING_1,
        spacing: { before: 0, after: 160 },
        children: [new TextRun({ text: "MỤC LỤC", bold: true, size: 28, color: COLOR_PRIMARY, font: "Arial" })]
      }),
      new TableOfContents("Mục lục", {
        hyperlink: true,
        headingStyleRange: "1-3",
      }),
      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 1: THÔNG TIN CHUNG =====
      heading1("1. THÔNG TIN CHUNG"),

      heading2("1.1 Base URL"),
      codeBlock(["https://facekhothuoc.nhidong.org.vn/api/camera-door"]),
      spacer(),

      heading2("1.2 Xác Thực (Security Key)"),
      para("Tất cả API đều yêu cầu Security Key. Có 2 cách truyền:"),
      spacer(60),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [2200, CONTENT_W - 2200],
        rows: [
          makeTableHeader(["Cách", "Ví dụ"], [2200, CONTENT_W - 2200]),
          makeTableRow(["Header (Khuyến nghị)", "X-Security-Key: YOUR_SECURITY_KEY"], [2200, CONTENT_W - 2200]),
          makeTableRow(["Query String", "?key=YOUR_SECURITY_KEY"], [2200, CONTENT_W - 2200], true),
        ]
      }),
      spacer(),
      para("Response khi thiếu hoặc sai key:", { bold: true }),
      codeBlock([
        '{',
        '  "success": false,',
        '  "message": "Security key khong hop le hoac bi thieu."',
        '}'
      ]),
      spacer(),

      heading2("1.3 Mã Lỗi HTTP"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1400, CONTENT_W - 1400],
        rows: [
          makeTableHeader(["HTTP Status", "Trường hợp"], [1400, CONTENT_W - 1400]),
          makeTableRow(["200", "Thành công."], [1400, CONTENT_W - 1400]),
          makeTableRow(["400", "Body/query không hợp lệ."], [1400, CONTENT_W - 1400], true),
          makeTableRow(["401", "Thiếu hoặc sai security key."], [1400, CONTENT_W - 1400]),
          makeTableRow(["500", "Lỗi server hoặc lỗi gọi dịch vụ bên dưới."], [1400, CONTENT_W - 1400], true),
        ]
      }),
      spacer(),

      heading2("1.4 Lưu Ý Lỗi 405 (PUT Bị Chặn Bởi IIS)"),
      para("Nếu IIS/WebDAV chặn HTTP verb PUT, sẽ nhận được trang HTML 405. Project đã bổ sung web.config để remove WebDAVModule. Cách gọi khuyến nghị cho hệ thống tích hợp:"),
      codeBlock(["POST /api/camera-door/control"]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 2: GROUPS =====
      heading1("2. API LẤY DANH SÁCH NHÓM CAMERA"),

      heading2("2.1 Endpoint"),
      endpointRow("GET", "/api/camera-door/groups"),
      spacer(),
      para("Lấy danh sách nhóm camera từ Hikvision theo devName."),
      spacer(60),

      heading2("2.2 Ví Dụ Request"),
      codeBlock([
        'curl -X GET "https://your-domain/api/camera-door/groups" \\',
        '  -H "X-Security-Key: YOUR_SECURITY_KEY"'
      ]),
      spacer(),

      heading2("2.3 Response Mẫu"),
      codeBlock([
        '{',
        '  "success": true,',
        '  "message": "Lay danh sach nhom camera thanh cong.",',
        '  "totalGroups": 8,',
        '  "totalCameras": 13,',
        '  "onlineCount": 13,',
        '  "offlineCount": 0,',
        '  "groups": [',
        '    {',
        '      "groupKey": "khochanthuocthuong",',
        '      "groupName": "KhoChanThuocThuong",',
        '      "cameraCount": 1,',
        '      "onlineCount": 1,',
        '      "offlineCount": 0,',
        '      "cameras": [',
        '        {',
        '          "devIndex": "abc123",',
        '          "ehomeId": "KhoChanThuocThuong",',
        '          "devName": "KhoChanThuocThuong",',
        '          "displayGroupName": "KhoChanThuocThuong",',
        '          "devSerial": "GK0230861",',
        '          "devMode": "DS-K1T341CMF",',
        '          "devType": "",',
        '          "devStatus": "online",',
        '          "isOnline": true',
        '        }',
        '      ]',
        '    }',
        '  ]',
        '}'
      ]),
      spacer(),

      heading2("2.4 Ghi Chú"),
      bullet("groupKey: key chuẩn hóa để nhóm dữ liệu."),
      bullet("groupName: tên nhóm camera hiển thị."),
      bullet("cameras[].ehomeId: mã camera ghi nhận trong dữ liệu điểm danh nếu thiết bị trả về EhomeID."),
      bullet("cameras[].devName: nhóm camera/kho đang gán trên Hikvision."),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 3: WAREHOUSE GROUPS =====
      heading1("3. API LẤY DANH SÁCH NHÓM CAMERA THEO KHO"),

      heading2("3.1 Endpoint"),
      endpointRow("GET", "/api/camera-door/warehouse-camera-groups"),
      spacer(),
      para("Lấy danh sách Id | Tên Kho đã gán camera. Cột id dùng để đối chiếu với cameraGroupId trong API điểm danh và làm giá trị filter nhomCamera."),
      spacer(60),

      heading2("3.2 Query Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1800, 1200, CONTENT_W - 3000],
        rows: [
          makeTableHeader(["Tham số", "Bắt buộc", "Mô tả"], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["khoId", "Không", "Lọc theo một kho cụ thể. Ví dụ: KhoChanThuocThuong."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["includeInactive", "Không", "true để lấy cả camera-kho đã inactive. Mặc định false."], [1800, 1200, CONTENT_W - 3000], true),
        ]
      }),
      spacer(),

      heading2("3.3 Ví Dụ Request"),
      codeBlock([
        'curl -X GET "https://your-domain/api/camera-door/warehouse-camera-groups" \\',
        '  -H "X-Security-Key: YOUR_SECURITY_KEY"'
      ]),
      spacer(),

      heading2("3.4 Response Mẫu"),
      codeBlock([
        '{',
        '  "success": true,',
        '  "message": "Lay danh sach nhom camera theo kho thanh cong.",',
        '  "total": 3,',
        '  "items": [',
        '    {',
        '      "id": "KhoChanThuocThuong",',
        '      "tenKho": "Kho chan thuoc thuong"',
        '    },',
        '    {',
        '      "id": "KhoChanThuocDB",',
        '      "tenKho": "Kho chan thuoc dac biet"',
        '    },',
        '    {',
        '      "id": "QuayThuocE1T4",',
        '      "tenKho": "Quay thuoc E1T4"',
        '    }',
        '  ]',
        '}'
      ]),
      spacer(),

      heading2("3.5 Cách Dùng Với API Điểm Danh"),
      bullet("Gọi /warehouse-camera-groups để lấy items[].id."),
      bullet("Truyền giá trị đó vào body của /attendance tại trường nhomCamera."),
      spacer(60),
      para("Ví dụ:", { bold: true }),
      codeBlock([
        '{',
        '  "nhomCamera": "KhoChanThuocThuong"',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 4: ATTENDANCE =====
      heading1("4. API LẤY DỮ LIỆU ĐIỂM DANH RA/VÀO KHO"),

      heading2("4.1 Endpoint"),
      endpointRow("POST", "/api/camera-door/attendance"),
      spacer(),
      para("API POST lấy dữ liệu điểm danh nhân viên/khách, trạng thái ra-vào kho, các lượt ra/vào và thông tin camera."),
      spacer(60),
      paraRuns([
        new TextRun({ text: "Lưu ý: ", bold: true, size: 20, font: "Arial" }),
        new TextRun({ text: "API đang gán cứng namHoc = \u201c2025-2026\u201d, client không cần truyền namHoc.", size: 20, font: "Arial" })
      ]),
      spacer(),

      heading2("4.2 Body Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1700, 1000, 1300, CONTENT_W - 4000],
        rows: [
          makeTableHeader(["Trường", "Kiểu", "Bắt buộc", "Mô tả"], [1700, 1000, 1300, CONTENT_W - 4000]),
          makeTableRow(["date", "string", "Không", "Ngày cần lấy dữ liệu. Hỗ trợ yyyy-MM-dd hoặc dd/MM/yyyy. Nếu bỏ trống lấy ngày hiện tại."], [1700, 1000, 1300, CONTENT_W - 4000]),
          makeTableRow(["ngay", "datetime", "Không", "Có thể dùng thay date. Nếu có cả hai, ưu tiên ngay."], [1700, 1000, 1300, CONTENT_W - 4000], true),
          makeTableRow(["maTruongBo", "string", "Nên truyền", "Mã trường/bệnh viện. Nếu bỏ trống API thử lấy từ session server."], [1700, 1000, 1300, CONTENT_W - 4000]),
          makeTableRow(["audience", "string", "Không", "staff, guest, all. Mặc định staff."], [1700, 1000, 1300, CONTENT_W - 4000], true),
          makeTableRow(["maPhongBan", "number", "Không", "Lọc theo khoa/phòng."], [1700, 1000, 1300, CONTENT_W - 4000]),
          makeTableRow(["trangThai", "number", "Không", "1: có vào kho, 0: chưa vào kho."], [1700, 1000, 1300, CONTENT_W - 4000], true),
          makeTableRow(["maNhanVien", "string", "Không", "Lọc theo mã nhân viên."], [1700, 1000, 1300, CONTENT_W - 4000]),
          makeTableRow(["nhomCamera", "string", "Không", "Lọc theo nhóm camera/kho. Giá trị lấy từ warehouse-camera-groups.items[].id."], [1700, 1000, 1300, CONTENT_W - 4000], true),
          makeTableRow(["includeCameraInfo", "bool", "Không", "true để map thêm thông tin camera. Mặc định true."], [1700, 1000, 1300, CONTENT_W - 4000]),
        ]
      }),
      spacer(),

      heading2("4.3 Ví Dụ Request"),
      para("Lấy toàn bộ nhân viên vào ngày 25/06/2026:", { bold: true }),
      codeBlock([
        'curl -X POST "https://your-domain/api/camera-door/attendance" \\',
        '  -H "Content-Type: application/json" \\',
        '  -H "X-Security-Key: YOUR_SECURITY_KEY" \\',
        '  -d \'{',
        '    "date": "2026-06-25",',
        '    "maTruongBo": "PN",',
        '    "audience": "staff"',
        '  }\''
      ]),
      spacer(80),
      para("Lọc theo nhân viên:", { bold: true }),
      codeBlock([
        '{',
        '  "date": "2026-06-25",',
        '  "maTruongBo": "PN",',
        '  "audience": "staff",',
        '  "maNhanVien": "NV001"',
        '}'
      ]),
      spacer(80),
      para("Lọc theo nhóm camera/kho:", { bold: true }),
      codeBlock([
        '{',
        '  "date": "2026-06-25",',
        '  "maTruongBo": "PN",',
        '  "audience": "all",',
        '  "nhomCamera": "KhoChanThuocThuong"',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      heading2("4.4 Response Mẫu"),
      codeBlock([
        '{',
        '  "thanhCong": true,',
        '  "thongBao": "Lay du lieu diem danh ra vao kho thanh cong.",',
        '  "ngay": "2026-06-25T00:00:00",',
        '  "maTruongBo": "PN",',
        '  "doiTuong": "staff",',
        '  "tongHop": {',
        '    "tongSo": 2,',
        '    "soNhanVien": 2,',
        '    "soKhach": 0,',
        '    "soCoVaoKho": 1,',
        '    "soChuaVaoKho": 1,',
        '    "soDaVaoKho": 1,',
        '    "soKhongVaoKho": 1,',
        '    "soKhachHetHan": 0,',
        '    "tongSoLanVaoKho": 1,',
        '    "thoiGianVaoDauTien": "08:15:00",',
        '    "thoiGianGhiNhanCuoi": "08:15:00",',
        '    "thoiGianTao": "2026-06-25T09:00:00+07:00"',
        '  },',
        '  "duLieu": [',
        '    {',
        '      "maNhanVien": "NV001",',
        '      "hoTen": "Nguyen Van A",',
        '      "maPhongBan": 12,',
        '      "tenPhongBan": "Khoa Duoc",',
        '      "ngay": "2026-06-25T00:00:00",',
        '      "laKhach": false,',
        '      "thoiGianHetHan": null,',
        '      "daHetHan": false,',
        '      "trangThaiDiemDanh": 1,',
        '      "coVaoKho": true,',
        '      "trangThaiHieuLuc": "VALID",',
        '      "trangThaiKho": "DA_VAO_KHO",',
        '      "trangThaiNghiepVu": "DA_VAO_KHO",',
        '      "moTaTrangThai": "Da vao kho",',
        '      "thoiGianVaoDauTien": "08:15:00",',
        '      "thoiGianGhiNhanCuoi": "08:15:00",',
        '      "soLanVaoKho": 1,',
        '      "danhSachLanVaoKho": [',
        '        {',
        '          "stt": 1,',
        '          "thoiGian": "08:15:00",',
        '          "thoiGianText": "08:15:00",',
        '          "loaiGhiNhan": "VAO_KHO",',
        '          "tenLoaiGhiNhan": "Vao kho",',
        '          "maCameraDiemDanh": "KhoChanThuocThuong",',
        '          "tenCamera": "KhoChanThuocThuong",',
        '          "maNhomCamera": "KhoChanThuocThuong",',
        '          "tenNhomCamera": "KhoChanThuocThuong",',
        '          "devIndex": "abc123",',
        '          "serial": "GK0230861",',
        '          "daNhanDienCamera": true,',
        '          "cameraOnline": true',
        '        }',
        '      ]',
        '    }',
        '  ]',
        '}'
      ]),
      spacer(),

      heading2("4.5 Ý Nghĩa Trạng Thái"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [2000, 2200, CONTENT_W - 4200],
        rows: [
          makeTableHeader(["Trường", "Giá trị", "Ý nghĩa"], [2000, 2200, CONTENT_W - 4200]),
          makeTableRow(["trangThaiDiemDanh", "1", "Có dữ liệu vào kho trong ngày."], [2000, 2200, CONTENT_W - 4200]),
          makeTableRow(["trangThaiDiemDanh", "0", "Chưa có dữ liệu vào kho trong ngày."], [2000, 2200, CONTENT_W - 4200], true),
          makeTableRow(["trangThaiKho", "CHUA_VAO_KHO", "Chưa vào kho."], [2000, 2200, CONTENT_W - 4200]),
          makeTableRow(["trangThaiKho", "DA_VAO_KHO", "Đã có log vào kho."], [2000, 2200, CONTENT_W - 4200], true),
          makeTableRow(["trangThaiNghiepVu", "CHUA_VAO_KHO", "Chưa vào kho."], [2000, 2200, CONTENT_W - 4200]),
          makeTableRow(["trangThaiNghiepVu", "DA_VAO_KHO", "Đã có log vào kho."], [2000, 2200, CONTENT_W - 4200], true),
          makeTableRow(["trangThaiNghiepVu", "EXPIRED_GUEST", "Khách đã hết hạn."], [2000, 2200, CONTENT_W - 4200]),
        ]
      }),
      spacer(),

      heading2("4.6 Quy Tắc Dữ Liệu Vào Kho"),
      bullet("API sắp xếp các lượt điểm danh theo thời gian tăng dần."),
      bullet("Mỗi log camera được xem là một VAO_KHO."),
      bullet("API không suy luận vào/ra theo lượt lẻ/chẵn."),
      bullet("trangThaiKho = DA_VAO_KHO khi nhân viên có ít nhất một log phù hợp bộ lọc."),
      bullet("trangThaiKho = CHUA_VAO_KHO khi không có log vào kho."),
      spacer(),

      heading2("4.7 Ghi Chú Lọc nhomCamera"),
      bullet("Trường nhomCamera nên truyền bằng id lấy từ API /warehouse-camera-groups."),
      bullet("Ví dụ nhomCamera = \u201cKhoChanThuocThuong\u201d."),
      bullet("API sẽ lọc các lượt có danhSachLanVaoKho[].maNhomCamera = \u201cKhoChanThuocThuong\u201d."),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 5: OPEN DOOR =====
      heading1("5. API MỞ CỬA NHANH"),

      heading2("5.1 Endpoint"),
      endpointRow("PUT", "/api/camera-door/open"),
      spacer(80),
      endpointRow("POST", "/api/camera-door/open"),
      spacer(),
      para("API mở cửa nhanh. Nội bộ sẽ chuyển thành lệnh cmd = \u201copen\u201d và gọi API control."),
      spacer(60),

      heading2("5.2 Body Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1700, 1000, 1200, CONTENT_W - 3900],
        rows: [
          makeTableHeader(["Trường", "Kiểu", "Bắt buộc", "Mô tả"], [1700, 1000, 1200, CONTENT_W - 3900]),
          makeTableRow(["devIndex", "string", "Có", "Mã thiết bị camera/door trên Hikvision."], [1700, 1000, 1200, CONTENT_W - 3900]),
          makeTableRow(["doorNo", "number", "Không", "Số cửa. Nếu bỏ trống hoặc <= 0, mặc định 1."], [1700, 1000, 1200, CONTENT_W - 3900], true),
          makeTableRow(["requestedBy", "string", "Không", "Người yêu cầu."], [1700, 1000, 1200, CONTENT_W - 3900]),
          makeTableRow(["userId", "string", "Không", "ID user nếu có."], [1700, 1000, 1200, CONTENT_W - 3900], true),
          makeTableRow(["note", "string", "Không", "Ghi chú."], [1700, 1000, 1200, CONTENT_W - 3900]),
        ]
      }),
      spacer(),

      heading2("5.3 Ví Dụ Request"),
      codeBlock([
        'curl -X PUT "https://your-domain/api/camera-door/open" \\',
        '  -H "Content-Type: application/json" \\',
        '  -d \'{',
        '    "devIndex": "abc123",',
        '    "doorNo": 1,',
        '    "requestedBy": "External API",',
        '    "userId": "00000000-0000-0000-0000-000000000000",',
        '    "note": "Mo cua tu he thong tich hop"',
        '  }\''
      ]),
      spacer(),

      heading2("5.4 Response Thành Công"),
      codeBlock([
        '{',
        '  "success": true,',
        '  "message": "Dieu khien cua thanh cong.",',
        '  "result": {',
        '    "success": true,',
        '    "statusCode": 200,',
        '    "responseBody": "..."',
        '  }',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 6: CONTROL DOOR =====
      heading1("6. API ĐIỀU KHIỂN CỬA"),

      heading2("6.1 Endpoint"),
      endpointRow("PUT", "/api/camera-door/control"),
      spacer(80),
      endpointRow("POST", "/api/camera-door/control"),
      spacer(),
      para("API điều khiển cửa theo command."),
      spacer(60),

      heading2("6.2 Body Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1700, 1000, 1200, CONTENT_W - 3900],
        rows: [
          makeTableHeader(["Trường", "Kiểu", "Bắt buộc", "Mô tả"], [1700, 1000, 1200, CONTENT_W - 3900]),
          makeTableRow(["devIndex", "string", "Có", "Mã thiết bị camera/door trên Hikvision."], [1700, 1000, 1200, CONTENT_W - 3900]),
          makeTableRow(["cmd", "string", "Có", "Lệnh điều khiển. Xem bảng cmd bên dưới."], [1700, 1000, 1200, CONTENT_W - 3900], true),
          makeTableRow(["doorNo", "number", "Không", "Số cửa. Bỏ trống hoặc <= 0, mặc định 1."], [1700, 1000, 1200, CONTENT_W - 3900]),
          makeTableRow(["requestedBy", "string", "Không", "Người yêu cầu."], [1700, 1000, 1200, CONTENT_W - 3900], true),
          makeTableRow(["userId", "string", "Không", "ID user nếu có."], [1700, 1000, 1200, CONTENT_W - 3900]),
          makeTableRow(["note", "string", "Không", "Ghi chú."], [1700, 1000, 1200, CONTENT_W - 3900], true),
        ]
      }),
      spacer(),

      heading2("6.3 Các Giá Trị CMD"),
      new Table({
        width: { size: CONTENT_W / 2, type: WidthType.DXA },
        columnWidths: [1600, CONTENT_W / 2 - 1600],
        rows: [
          makeTableHeader(["cmd", "Ý nghĩa"], [1600, CONTENT_W / 2 - 1600]),
          makeTableRow(["open", "Mở cửa"], [1600, CONTENT_W / 2 - 1600]),
          makeTableRow(["close", "Đóng cửa"], [1600, CONTENT_W / 2 - 1600], true),
          makeTableRow(["alwaysOpen", "Luôn mở cửa"], [1600, CONTENT_W / 2 - 1600]),
          makeTableRow(["alwaysClose", "Luôn đóng cửa"], [1600, CONTENT_W / 2 - 1600], true),
        ]
      }),
      spacer(),

      heading2("6.4 Ví Dụ Request"),
      para("Mở cửa:", { bold: true }),
      codeBlock([
        '{',
        '  "devIndex": "abc123",',
        '  "cmd": "open",',
        '  "doorNo": 1,',
        '  "requestedBy": "External API",',
        '  "note": "Mo cua kho"',
        '}'
      ]),
      spacer(80),
      para("Luôn đóng cửa:", { bold: true }),
      codeBlock([
        '{',
        '  "devIndex": "abc123",',
        '  "cmd": "alwaysClose",',
        '  "doorNo": 1,',
        '  "requestedBy": "External API",',
        '  "note": "Khoa cua sau gio lam viec"',
        '}'
      ]),
      spacer(80),
      para("Ví dụ thực tế (đóng cửa kho - dùng POST):", { bold: true }),
      codeBlock([
        'curl -X POST "https://facekhothuoc.nhidong.org.vn/api/camera-door/control" \\',
        '  -H "Content-Type: application/json" \\',
        '  -H "X-Security-Key: b9X#4qLm2@Np7VzR8!sKd1Yw6TcH" \\',
        '  -d \'{',
        '    "devIndex": "41C7A5E3-8F85-4807-8BDE-A688F677184B",',
        '    "cmd": "close",',
        '    "doorNo": 1,',
        '    "requestedBy": "Thai",',
        '    "note": "dong cua kho"',
        '  }\''
      ]),
      spacer(),

      heading2("6.5 Response"),
      para("Thành công:", { bold: true }),
      codeBlock([
        '{',
        '  "success": true,',
        '  "message": "Dieu khien cua thanh cong.",',
        '  "result": {',
        '    "success": true,',
        '    "statusCode": 200,',
        '    "responseBody": "...",',
        '    "devIndex": "41C7A5E3-8F85-4807-8BDE-A688F677184B",',
        '    "command": "close",',
        '    "doorNo": 1',
        '  }',
        '}'
      ]),
      spacer(80),
      para("Lỗi từ Hikvision:", { bold: true }),
      codeBlock([
        '{',
        '  "success": false,',
        '  "message": "Hikvision API tra ve loi.",',
        '  "result": {',
        '    "success": false,',
        '    "statusCode": 400,',
        '    "responseBody": "..."',
        '  }',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 7: AUDIT LOG =====
      heading1("7. HEADER GHI LOG CHO API ĐIỀU KHIỂN CỬA"),
      para("Khi gọi API /open hoặc /control, hệ thống ghi audit log. Có thể truyền thêm header để log rõ người thao tác:"),
      spacer(80),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [2800, CONTENT_W - 2800],
        rows: [
          makeTableHeader(["Header", "Mô tả"], [2800, CONTENT_W - 2800]),
          makeTableRow(["X-User-Name", "Tên người thao tác. Ví dụ: Nguyen Van A"], [2800, CONTENT_W - 2800]),
          makeTableRow(["X-Requested-By", "Tên hệ thống gọi. Ví dụ: External System"], [2800, CONTENT_W - 2800], true),
          makeTableRow(["X-User-Id", "UUID của user. Ví dụ: 00000000-0000-0000-0000-000000000000"], [2800, CONTENT_W - 2800]),
        ]
      }),
      spacer(),
      paraRuns([
        new TextRun({ text: "Lưu ý: ", bold: true, size: 20, font: "Arial" }),
        new TextRun({ text: "Nếu body có requestedBy thì ưu tiên giá trị này.", size: 20, font: "Arial" })
      ]),
      codeBlock([
        'X-User-Name: Nguyen Van A',
        'X-Requested-By: External System',
        'X-User-Id: 00000000-0000-0000-0000-000000000000'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 8: INTEGRATION FLOW =====
      heading1("8. LUỒNG TÍCH HỢP GỢI Ý"),
      para("Luồng khuyến nghị khi tích hợp với hệ thống bên ngoài:"),
      spacer(80),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [800, 2600, CONTENT_W - 3400],
        rows: [
          makeTableHeader(["Bước", "Hành động", "Chi tiết"], [800, 2600, CONTENT_W - 3400]),
          makeTableRow(["1", "Lấy danh sách kho", "GET /api/camera-door/warehouse-camera-groups"], [800, 2600, CONTENT_W - 3400]),
          makeTableRow(["2", "Chọn kho", "Lấy items[].id từ kết quả bước 1."], [800, 2600, CONTENT_W - 3400], true),
          makeTableRow(["3", "Lấy dữ liệu điểm danh", "POST /api/camera-door/attendance với nhomCamera = id."], [800, 2600, CONTENT_W - 3400]),
          makeTableRow(["4", "Lấy devIndex thiết bị", "Lấy từ API /groups hoặc từ dữ liệu camera đã có."], [800, 2600, CONTENT_W - 3400], true),
          makeTableRow(["5", "Điều khiển cửa", "POST /api/camera-door/open hoặc /control."], [800, 2600, CONTENT_W - 3400]),
        ]
      }),
      spacer(200),
      para("Lưu ý về HTTP verb:", { bold: true }),
      bullet("Khuyến nghị dùng POST thay vì PUT để tránh lỗi 405 từ IIS/WebDAV."),
      bullet("Nếu server đã cấu hình web.config remove WebDAVModule, có thể dùng cả PUT và POST."),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 9: MÃ LỖI CHUNG =====
      heading1("9. MÃ LỖI CHUNG"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1400, CONTENT_W - 1400],
        rows: [
          makeTableHeader(["HTTP Status", "Trường hợp"], [1400, CONTENT_W - 1400]),
          makeTableRow(["200", "Thành công."], [1400, CONTENT_W - 1400]),
          makeTableRow(["400", "Body/query không hợp lệ."], [1400, CONTENT_W - 1400], true),
          makeTableRow(["401", "Thiếu hoặc sai security key."], [1400, CONTENT_W - 1400]),
          makeTableRow(["500", "Lỗi server hoặc lỗi gọi dịch vụ bên dưới."], [1400, CONTENT_W - 1400], true),
        ]
      }),
      spacer(),

      // ===== SECTION 10: LỖI 405 =====
      heading1("10. LƯU Ý LỖI 405 KHI GỌI PUT"),
      para("Nếu Postman nhận trang HTML với nội dung:"),
      codeBlock(["405 - HTTP verb used to access this page is not allowed."]),
      spacer(80),
      para("Nguyên nhân thường gặp là IIS/WebDAV chặn HTTP verb PUT trước khi request vào ASP.NET Core. Project đã bổ sung web.config để remove WebDAVModule/WebDAV handler khi publish."),
      spacer(60),
      para("Cách gọi khuyến nghị cho hệ thống tích hợp:", { bold: true }),
      codeBlock(["POST /api/camera-door/control"]),
      spacer(80),
      para("Body giữ nguyên:", { bold: true }),
      codeBlock([
        '{',
        '  "devIndex": "abc6F566C8E-1F3F-403E-956C-635F6A89C58A123",',
        '  "cmd": "close",',
        '  "doorNo": 1,',
        '  "requestedBy": "Thai",',
        '  "note": "dong cua kho"',
        '}'
      ]),
      spacer(200),

      // Footer note
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 400, after: 0 },
        border: { top: { style: BorderStyle.SINGLE, size: 4, color: COLOR_PRIMARY, space: 8 } },
        children: [new TextRun({ text: "Công ty Cổ phần TITKUL  |  Tài liệu nội bộ  |  Phiên bản 1.0  |  06/2026", size: 18, font: "Arial", color: "888888" })]
      }),
    ]
  }]
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync('/home/claude/API_DOCS_VI_final.docx', buffer);
  console.log('Done!');
});