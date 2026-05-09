// ── SCN Kế Hoạch Chủ Nhiệm – Quill Rich-Text Helpers ────────────────────────────
// Hỗ trợ 3 tab: dacdiem | kehoach | congtac – mỗi tab có 1 Quill instance độc lập
(function () {
    'use strict';

    // Map instanceKey -> Quill instance
    var _instances = {};

    /**
     * Dọn dẹp hoàn toàn DOM do Quill tạo ra trong một wrapper element.
     * Quill biến đổi <div id="editor"> thành .ql-editor bên trong .ql-container
     * và chèn thêm .ql-toolbar là sibling trước đó.
     * Hàm này khôi phục lại một <div id="..."> sạch để có thể khởi tạo lại.
     */
    function cleanupQuillDom(editorId) {
        var el = document.getElementById(editorId);
        if (!el) return null;

        var parent = el.parentNode;
        if (!parent) {
            el.innerHTML = '';
            return el;
        }

        // Xóa tất cả .ql-toolbar (Quill chèn trước .ql-container)
        var toolbars = parent.querySelectorAll('.ql-toolbar');
        toolbars.forEach(function (t) { t.remove(); });

        // Kiểm tra xem el đã bị Quill bọc thành .ql-editor chưa
        // Nếu có, .ql-container là child của parent thay thế el gốc
        var container = parent.querySelector('.ql-container');
        if (container) {
            // Tạo một div mới sạch với cùng id để thay thế .ql-container
            var freshDiv = document.createElement('div');
            freshDiv.id = editorId;
            container.replaceWith(freshDiv);
            return freshDiv;
        }

        // El vẫn là div gốc chưa bị Quill xử lý, chỉ cần xóa nội dung
        el.innerHTML = '';
        return el;
    }

    window.scnKhcn = {

        // Khởi tạo Quill cho element có id = editorId với instanceKey để phân biệt
        init: function (editorId, instanceKey, placeholder, content) {
            // Hủy instance cũ nếu trùng key
            if (_instances[instanceKey]) {
                _instances[instanceKey] = null;
                delete _instances[instanceKey];
            }

            // Dọn dẹp DOM cũ trước khi init mới (tránh stack toolbar)
            var el = cleanupQuillDom(editorId);
            if (!el) {
                console.warn('[scnKhcn] Element #' + editorId + ' not found.');
                return;
            }

            var q = new Quill('#' + editorId, {
                theme: 'snow',
                placeholder: placeholder || 'Nhập nội dung...',
                modules: {
                    toolbar: [
                        [{ 'header': [1, 2, 3, false] }],
                        ['bold', 'italic', 'underline', 'strike'],
                        [{ 'color': [] }, { 'background': [] }],
                        [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                        [{ 'indent': '-1' }, { 'indent': '+1' }],
                        [{ 'align': [] }],
                        ['blockquote'],
                        ['clean']
                    ]
                }
            });

            // Đặt nội dung ban đầu
            if (content && content.trim() !== '') {
                q.root.innerHTML = content;
            }

            _instances[instanceKey] = q;
        },

        // Trả về nội dung HTML của instance theo key
        getContent: function (instanceKey) {
            var q = _instances[instanceKey];
            if (!q) return '';
            var html = q.root.innerHTML;
            return (html === '<p><br></p>') ? '' : html;
        },

        // Ghi đè nội dung HTML vào editor
        setContent: function (instanceKey, html) {
            var q = _instances[instanceKey];
            if (!q) return;
            q.root.innerHTML = html || '';
        },

        // Hủy instance và dọn DOM theo key
        destroy: function (instanceKey, editorId) {
            if (_instances[instanceKey]) {
                _instances[instanceKey] = null;
                delete _instances[instanceKey];
            }
            if (editorId) {
                cleanupQuillDom(editorId);
            }
        },

        // Hủy tất cả instances (chỉ null reference, DOM sẽ được dọn khi init lại)
        destroyAll: function () {
            _instances = {};
        },

        // Hủy tất cả và dọn DOM của các editorId cho trước
        destroyAllWithDom: function (editorIds) {
            _instances = {};
            if (Array.isArray(editorIds)) {
                editorIds.forEach(function (id) {
                    cleanupQuillDom(id);
                });
            }
        }
    };
})();
