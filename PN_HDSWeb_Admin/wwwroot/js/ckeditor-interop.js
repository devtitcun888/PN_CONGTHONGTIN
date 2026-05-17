// ckeditor-interop.js
let editors = {};

function getClassicEditor() {
    if (window.ClassicEditor) {
        return window.ClassicEditor;
    }
    // Bản GPL chỉ có window.ClassicEditor, không cần kiểm tra window.CKEDITOR
    return null;
}

function waitForClassicEditor(timeoutMs = 10000) {
    return new Promise((resolve, reject) => {
        if (getClassicEditor()) {
            resolve();
            return;
        }

        const started = Date.now();
        const timer = setInterval(() => {
            if (getClassicEditor()) {
                clearInterval(timer);
                resolve();
                return;
            }

            if (Date.now() - started >= timeoutMs) {
                clearInterval(timer);
                reject(new Error('CKEditor script did not finish loading in time.'));
            }
        }, 50);
    });
}

export async function initializeEditor(editorId, placeholder) {
    const element = document.getElementById(editorId);
    if (!element) {
        throw new Error(`Editor element ${editorId} was not found.`);
    }

    await waitForClassicEditor();

    const ClassicEditor = getClassicEditor();
    if (!ClassicEditor) {
        throw new Error('CKEditor build is not available after script load.');
    }

    if (editors[editorId]) {
        return editors[editorId];
    }

    const editor = await ClassicEditor.create(element, {
        placeholder: placeholder,
        toolbar: {
            items: [
                'heading', '|',
                'bold', 'italic', 'underline', 'strikethrough', '|',
                'fontSize', 'fontFamily', 'fontColor', 'fontBackgroundColor', '|',
                'alignment', '|',
                'numberedList', 'bulletedList', '|',
                'outdent', 'indent', '|',
                'link', 'imageUpload', 'mediaEmbed', '|',
                'undo', 'redo', '|',
                'findAndReplace', 'selectAll', '|',
                'table', 'blockQuote', '|',
                'sourceEditing'
            ]
        },
        heading: {
            options: [
                { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
                { model: 'heading1', view: 'h1', title: 'Heading 1', class: 'ck-heading_heading1' },
                { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
                { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' }
            ]
        },
        list: {
            properties: {
                styles: true,
                startIndex: true,
                reversed: true
            }
        },
        alignment: {
            options: ['left', 'center', 'right', 'justify']
        },
        language: 'vi',
        image: {
            toolbar: [
                'imageTextAlternative',
                'toggleImageCaption',
                'imageStyle:inline',
                'imageStyle:block',
                'imageStyle:side'
            ]
        }
    });

    editors[editorId] = editor;
    return editor;
}

export function getEditorData(editorId) {
    const editor = editors[editorId];
    if (editor) {
        return editor.getData();
    }
    return '';
}

export function setEditorData(editorId, data) {
    const editor = editors[editorId];
    if (editor) {
        editor.setData(data || '');
        return true;
    }
    return false;
}

export function destroyEditor(editor) {
    if (editor && editor.destroy) {
        editor.destroy();
    }
}