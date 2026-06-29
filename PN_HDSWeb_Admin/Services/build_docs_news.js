
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  AlignmentType, LevelFormat, HeadingLevel, BorderStyle, WidthType, ShadingType,
  VerticalAlign, PageNumber, PageBreak, Header, Footer,
  TableOfContents
} = require('docx');
const fs = require('fs');
const path = require('path');

// Colors
const COLOR_PRIMARY = "1B6EC2";
const COLOR_HEADER_BG = "1B6EC2";
const COLOR_HEADER_TEXT = "FFFFFF";
const COLOR_ROW_ALT = "F3F8FC";
const COLOR_CODE_BG = "F7F9FB";
const COLOR_BORDER = "D1E3F5";
const COLOR_GET = "2E7D32";
const COLOR_POST = "1565C0";
const COLOR_PUT = "EF6C00";
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

function heading3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 200, after: 80 },
    children: [new TextRun({ text, bold: true, size: 22, color: "333333", font: "Arial" })]
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
              top: { style: BorderStyle.SINGLE, size: 1, color: "A5D6A7" },
              bottom: { style: BorderStyle.SINGLE, size: 1, color: "A5D6A7" },
              left: { style: BorderStyle.THICK, size: 12, color: COLOR_PRIMARY },
              right: { style: BorderStyle.SINGLE, size: 1, color: "A5D6A7" },
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
      {
        id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 28, bold: true, font: "Arial", color: COLOR_PRIMARY },
        paragraph: { spacing: { before: 360, after: 120 }, outlineLevel: 0 }
      },
      {
        id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 24, bold: true, font: "Arial", color: COLOR_PRIMARY },
        paragraph: { spacing: { before: 280, after: 100 }, outlineLevel: 1 }
      },
      {
        id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 22, bold: true, font: "Arial" },
        paragraph: { spacing: { before: 200, after: 80 }, outlineLevel: 2 }
      },
    ]
  },
  numbering: {
    config: [
      {
        reference: "bullets",
        levels: [{
          level: 0, format: LevelFormat.BULLET, text: "\u2022", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } }
        }]
      },
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
              new TextRun({ text: "TÀI LIỆU ĐẶC TẢ API  |  Public Portal (Tin tức, Văn bản & Danh bạ)  |  ", size: 18, font: "Arial", color: "666666" }),
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
        children: [new TextRun({ text: "Public Portal API - Tin tức, Văn bản & Danh bạ", size: 32, font: "Arial", color: "37474F" })]
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 200 },
        border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: COLOR_PRIMARY, space: 8 } },
        children: []
      }),

      ...([
        ["Môi trường", "Development / Production"],
        ["Xác thực", "Không yêu cầu (Public - AllowAnonymous)"],
        ["Base Path", "/api/public"],
        ["Phiên bản", "1.0.0"],
        ["Cập nhật", "06/2026"],
      ].map(([k, v]) => infoBox(k, v, ["Base Path"].includes(k)))),

      spacer(400),
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
      codeBlock(["https://[domain]/api/public"]),
      spacer(),

      heading2("1.2 Cơ Chế Tham Số maTruongBo"),
      para("Hầu hết các API public đều yêu cầu lọc theo mã đơn vị / mã trường (maTruongBo)."),
      bullet("Client có thể truyền tham số maTruongBo dưới dạng Query Parameter (ví dụ: ?maTruongBo=PN)."),
      bullet("Nếu bỏ trống hoặc không truyền, hệ thống sẽ tự động sử dụng giá trị cấu hình mặc định lưu trữ trong file config của backend (PN_PublicVariables.MaTruong)."),
      spacer(),

      heading2("1.3 Mã Lỗi HTTP"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1400, CONTENT_W - 1400],
        rows: [
          makeTableHeader(["HTTP Status", "Trường hợp áp dụng"], [1400, CONTENT_W - 1400]),
          makeTableRow(["200 OK", "Yêu cầu thành công, dữ liệu được trả về dạng JSON camelCase."], [1400, CONTENT_W - 1400]),
          makeTableRow(["400 Bad Request", "Tham số truyền lên không đúng định dạng."], [1400, CONTENT_W - 1400], true),
          makeTableRow(["404 Not Found", "Không tìm thấy tài nguyên theo yêu cầu (Slug/ID không tồn tại)."], [1400, CONTENT_W - 1400]),
          makeTableRow(["500 Internal Error", "Gặp sự cố lỗi hệ thống phía Server."], [1400, CONTENT_W - 1400], true),
        ]
      }),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 2: CATEGORIES =====
      heading1("2. API DANH MỤC BÀI VIẾT (POST CATEGORIES)"),

      heading2("2.1 Lấy danh sách danh mục bài viết"),
      endpointRow("GET", "/api/public/posts/categories"),
      spacer(),
      para("Trả về toàn bộ danh mục tin tức hoạt động ở trạng thái active."),
      spacer(60),
      heading3("Ví dụ Request"),
      codeBlock([
        'curl -X GET "https://[domain]/api/public/posts/categories?maTruongBo=PN"'
      ]),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '[',
        '  {',
        '    "id": "cat_001",',
        '    "categoryName": "Tin hoạt động",',
        '    "slug": "tin-hoat-dong",',
        '    "parentId": null,',
        '    "description": "Các tin tức về hoạt động của nhà trường",',
        '    "sortOrder": 1',
        '  }',
        ']'
      ]),
      spacer(),

      heading2("2.2 Lấy chi tiết danh mục theo Slug"),
      endpointRow("GET", "/api/public/posts/categories/{slug}"),
      spacer(),
      para("Lấy thông tin chi tiết của một danh mục cụ thể bằng slug."),
      spacer(60),
      heading3("Ví dụ Request"),
      codeBlock([
        'curl -X GET "https://[domain]/api/public/posts/categories/tin-hoat-dong?maTruongBo=PN"'
      ]),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '{',
        '  "id": "cat_001",',
        '  "categoryName": "Tin hoạt động",',
        '  "slug": "tin-hoat-dong",',
        '  "parentId": null,',
        '  "description": "Các tin tức về hoạt động của nhà trường",',
        '  "sortOrder": 1',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 3: POSTS =====
      heading1("3. API TIN TỨC & BÀI VIẾT (POSTS)"),

      heading2("3.1 Lấy danh sách bài viết (Có phân trang, tìm kiếm & lọc)"),
      endpointRow("GET", "/api/public/posts"),
      spacer(),
      para("Lấy danh sách các bài viết đã xuất bản. Hỗ trợ tìm kiếm từ khóa và lọc danh mục."),
      spacer(60),
      heading3("Query Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1800, 1200, CONTENT_W - 3000],
        rows: [
          makeTableHeader(["Tham số", "Mặc định", "Mô tả"], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["categoryId", "Bỏ trống", "Lọc theo mã danh mục bài viết."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["keyword", "Bỏ trống", "Tìm kiếm theo tiêu đề hoặc tóm tắt bài viết."], [1800, 1200, CONTENT_W - 3000], true),
          makeTableRow(["page", "1", "Trang hiện tại."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["pageSize", "10", "Số lượng bài viết trên mỗi trang."], [1800, 1200, CONTENT_W - 3000], true),
          makeTableRow(["maTruongBo", "Mặc định hệ thống", "Mã đơn vị/trường học cần truy vấn."], [1800, 1200, CONTENT_W - 3000]),
        ]
      }),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '{',
        '  "items": [',
        '    {',
        '      "id": "post_001",',
        '      "title": "Lễ tổng kết năm học 2025-2026",',
        '      "slug": "le-tong-ket-nam-hoc-2025-2026",',
        '      "summary": "Nhà trường tổ chức lễ tổng kết năm học...",',
        '      "coverImageUrl": "/uploads/posts/cover.jpg",',
        '      "publishAt": "2026-06-25T08:00:00Z",',
        '      "viewCount": 245,',
        '      "categoryId": "cat_001",',
        '      "categoryName": "Tin hoạt động",',
        '      "categorySlug": "tin-hoat-dong",',
        '      "tags": []',
        '    }',
        '  ],',
        '  "totalItems": 45,',
        '  "page": 1,',
        '  "pageSize": 10,',
        '  "totalPages": 5',
        '}'
      ]),
      spacer(),

      heading2("3.2 Lấy chi tiết bài viết theo Slug"),
      endpointRow("GET", "/api/public/posts/{slug}"),
      spacer(),
      para("Lấy chi tiết nội dung HTML bài viết, danh sách tag và toàn bộ tài liệu đính kèm (media/attachments)."),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '{',
        '  "id": "post_001",',
        '  "title": "Lễ tổng kết năm học 2025-2026",',
        '  "slug": "le-tong-ket-nam-hoc-2025-2026",',
        '  "summary": "Nhà trường tổ chức lễ tổng kết năm học...",',
        '  "content": "<p>Nội dung chi tiết của bài viết được định dạng HTML ở đây...</p>",',
        '  "coverImageUrl": "/uploads/posts/cover.jpg",',
        '  "publishAt": "2026-06-25T08:00:00Z",',
        '  "viewCount": 245,',
        '  "categoryId": "cat_001",',
        '  "categoryName": "Tin hoạt động",',
        '  "categorySlug": "tin-hoat-dong",',
        '  "tags": [',
        '    {',
        '      "id": "tag_001",',
        '      "tagName": "Lễ tổng kết",',
        '      "slug": "le-tong-ket"',
        '    }',
        '  ],',
        '  "attachments": [',
        '    {',
        '      "id": "media_001",',
        '      "mediaType": "pdf",',
        '      "fileName": "QuyetDinhKhenThuong.pdf",',
        '      "fileUrl": "/uploads/posts/QuyetDinhKhenThuong.pdf",',
        '      "thumbnailUrl": null,',
        '      "fileSize": 1048576,',
        '      "mimeType": "application/pdf",',
        '      "sortOrder": 1,',
        '      "caption": "Quyết định khen thưởng học sinh giỏi"',
        '    }',
        '  ]',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      heading2("3.3 Lấy bài viết liên quan (Related Posts)"),
      endpointRow("GET", "/api/public/posts/{slug}/related"),
      spacer(),
      para("Lấy danh sách các bài viết cùng danh mục với bài viết hiện tại (loại trừ bài viết hiện tại)."),
      spacer(60),
      heading3("Query Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1800, 1200, CONTENT_W - 3000],
        rows: [
          makeTableHeader(["Tham số", "Mặc định", "Mô tả"], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["take", "4", "Số lượng bài viết liên quan tối đa cần lấy."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["maTruongBo", "Mặc định hệ thống", "Mã đơn vị/trường học."], [1800, 1200, CONTENT_W - 3000], true),
        ]
      }),
      spacer(60),
      heading3("Response Mẫu"),
      para("Mảng danh sách bài viết (tương tự trường `items` trong API 3.1)."),
      spacer(),

      heading2("3.4 Lấy bài viết xem nhiều nhất (Popular Posts)"),
      endpointRow("GET", "/api/public/posts/popular"),
      spacer(),
      para("Lấy các bài viết có lượt xem cao nhất của trường."),
      spacer(60),
      heading3("Query Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1800, 1200, CONTENT_W - 3000],
        rows: [
          makeTableHeader(["Tham số", "Mặc định", "Mô tả"], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["take", "8", "Số lượng bài viết tối đa cần lấy."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["excludePostId", "Bỏ trống", "ID bài viết cần loại trừ khỏi danh sách phổ biến."], [1800, 1200, CONTENT_W - 3000], true),
          makeTableRow(["maTruongBo", "Mặc định hệ thống", "Mã đơn vị/trường học."], [1800, 1200, CONTENT_W - 3000]),
        ]
      }),
      spacer(),

      heading2("3.5 Lấy danh sách bài viết theo Tag"),
      endpointRow("GET", "/api/public/posts/tag/{tagSlug}"),
      spacer(),
      para("Lấy toàn bộ bài viết được gán nhãn tag tương ứng."),
      spacer(60),
      heading3("Query Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1800, 1200, CONTENT_W - 3000],
        rows: [
          makeTableHeader(["Tham số", "Mặc định", "Mô tả"], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["page", "1", "Số thứ tự trang."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["pageSize", "30", "Số bài viết trên một trang."], [1800, 1200, CONTENT_W - 3000], true),
          makeTableRow(["maTruongBo", "Mặc định hệ thống", "Mã đơn vị/trường học."], [1800, 1200, CONTENT_W - 3000]),
        ]
      }),
      spacer(),

      heading2("3.6 Tăng lượt xem bài viết"),
      endpointRow("POST", "/api/public/posts/{postId}/view"),
      spacer(),
      para("Tăng số lượt xem của bài viết cụ thể lên 1 đơn vị (sử dụng khi người dùng mở xem chi tiết)."),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '{',
        '  "success": true',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 4: DOCUMENTS =====
      heading1("4. API VĂN BẢN PHÁP QUY (PUBLIC DOCUMENTS)"),

      heading2("4.1 Lấy danh sách văn bản"),
      endpointRow("GET", "/api/public/documents"),
      spacer(),
      para("Lấy danh sách các văn bản, thông tư, quyết định công khai."),
      spacer(60),
      heading3("Query Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1800, 1200, CONTENT_W - 3000],
        rows: [
          makeTableHeader(["Tham số", "Mặc định", "Mô tả"], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["keyword", "Bỏ trống", "Tìm kiếm theo tiêu đề hoặc số hiệu văn bản."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["documentTypeId", "Bỏ trống", "Lọc theo mã loại văn bản."], [1800, 1200, CONTENT_W - 3000], true),
          makeTableRow(["page", "1", "Trang số."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["pageSize", "10", "Số lượng dòng trên mỗi trang."], [1800, 1200, CONTENT_W - 3000], true),
          makeTableRow(["maTruongBo", "Mặc định hệ thống", "Mã đơn vị/trường học."], [1800, 1200, CONTENT_W - 3000]),
        ]
      }),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '{',
        '  "items": [',
        '    {',
        '      "id": "doc_001",',
        '      "title": "Quy chế hoạt động năm học 2025-2026",',
        '      "documentNumber": "QC/2025/001",',
        '      "issuedAt": "2025-09-01T00:00:00Z",',
        '      "fileUrl": "/uploads/documents/quy-che-2025.pdf",',
        '      "documentTypeId": "type_01",',
        '      "typeName": "Quy chế",',
        '      "typeSlug": "quy-che"',
        '    }',
        '  ],',
        '  "totalItems": 20,',
        '  "page": 1,',
        '  "pageSize": 10,',
        '  "totalPages": 2',
        '}'
      ]),
      spacer(),

      heading2("4.2 Lấy chi tiết văn bản"),
      endpointRow("GET", "/api/public/documents/{id}"),
      spacer(),
      para("Lấy nội dung chi tiết và tệp tải xuống của văn bản theo ID."),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '{',
        '  "id": "doc_001",',
        '  "title": "Quy chế hoạt động năm học 2025-2026",',
        '  "documentNumber": "QC/2025/001",',
        '  "description": "Quy chế hoạt động chung của nhà trường...",',
        '  "content": "<p>Nội dung toàn văn...</p>",',
        '  "issuedAt": "2025-09-01T00:00:00Z",',
        '  "fileUrl": "/uploads/documents/quy-che-2025.pdf",',
        '  "documentTypeId": "type_01",',
        '  "typeName": "Quy chế",',
        '  "typeSlug": "quy-che",',
        '  "issuer": "Ban Giám Hiệu"',
        '}'
      ]),
      spacer(),

      new Paragraph({ children: [new PageBreak()] }),

      // ===== SECTION 5: STAFF PROFILES =====
      heading1("5. API DANH BẠ VÀ SƠ ĐỒ TỔ CHỨC (STAFF PROFILES)"),

      heading2("5.1 Lấy danh sách cán bộ giáo viên"),
      endpointRow("GET", "/api/public/staff-profiles"),
      spacer(),
      para("Lấy danh sách thông tin cán bộ giáo viên được công khai."),
      spacer(60),
      heading3("Query Parameters"),
      new Table({
        width: { size: CONTENT_W, type: WidthType.DXA },
        columnWidths: [1800, 1200, CONTENT_W - 3000],
        rows: [
          makeTableHeader(["Tham số", "Mặc định", "Mô tả"], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["keyword", "Bỏ trống", "Tìm kiếm theo họ tên, chức vụ hoặc khoa phòng."], [1800, 1200, CONTENT_W - 3000]),
          makeTableRow(["departmentId", "Bỏ trống", "Lọc theo phòng ban / tổ chuyên môn (ví dụ: Ban Giám Hiệu, Tổ toán...)."], [1800, 1200, CONTENT_W - 3000], true),
          makeTableRow(["maTruongBo", "Mặc định hệ thống", "Mã đơn vị/trường học."], [1800, 1200, CONTENT_W - 3000]),
        ]
      }),
      spacer(60),
      heading3("Response Mẫu"),
      codeBlock([
        '[',
        '  {',
        '    "id": "staff_001",',
        '    "fullName": "Nguyễn Văn A",',
        '    "position": "Hiệu trưởng",',
        '    "department": "Ban Giám Hiệu",',
        '    "email": "nguyenvana@truong.edu.vn",',
        '    "phone": "0909123456",',
        '    "avatarUrl": "/uploads/staff/nguyenvana.jpg",',
        '    "sortOrder": 1,',
        '    "qualification": "Thạc sĩ",',
        '    "certificateInfo": "Chứng chỉ quản lý giáo dục",',
        '    "bio": "Hơn 15 năm kinh nghiệm trong ngành giáo dục..."',
        '  }',
        ']'
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

const outputPath = path.join(__dirname, 'API_DOCS_NEWS.docx');

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync(outputPath, buffer);
  console.log(`Document saved successfully as: ${outputPath}`);
}).catch(err => {
  console.error("Error writing document:", err);
});
