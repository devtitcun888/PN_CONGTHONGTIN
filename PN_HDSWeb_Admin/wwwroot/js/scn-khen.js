// ── SCN Khen Thưởng – Quill Rich-Text Helpers ────────────────────────────
// Tích hợp với tab "Kết quả khen thưởng" trong SCN_KeHoachGiaoDuc.razor
(function () {
    'use strict';

    var _quill = null;

    window.scnKhen = {

        // Khởi tạo Quill cho element có id = editorId, điền sẵn content (HTML)
        init: function (editorId, content) {
            // Hủy instance cũ nếu có
            if (_quill) {
                _quill = null;
            }

            var el = document.getElementById(editorId);
            if (!el) {
                console.warn('[scnKhen] Element #' + editorId + ' not found.');
                return;
            }

            // Xóa nội dung cũ (nếu mount lại)
            el.innerHTML = '';

            _quill = new Quill('#' + editorId, {
                theme: 'snow',
                placeholder: 'Nhập nội dung khen thưởng...',
                modules: {
                    toolbar: [
                        [{ 'header': [1, 2, 3, false] }],
                        ['bold', 'italic', 'underline', 'strike'],
                        [{ 'color': [] }, { 'background': [] }],
                        [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                        [{ 'align': [] }],
                        ['blockquote'],
                        ['clean']
                    ]
                }
            });

            // Đặt nội dung ban đầu
            if (content && content.trim() !== '') {
                _quill.root.innerHTML = content;
            }
        },

        // Trả về nội dung HTML hiện tại trong editor
        getContent: function () {
            if (!_quill) return '';
            var html = _quill.root.innerHTML;
            // Không trả "<p><br></p>" khi editor trống
            return (html === '<p><br></p>') ? '' : html;
        },

        // Ghi đè nội dung HTML vào editor
        setContent: function (html) {
            if (!_quill) return;
            _quill.root.innerHTML = html || '';
        },

        // Hủy instance (khi component unmount)
        destroy: function () {
            _quill = null;
        }
    };
})();
