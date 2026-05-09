// ckeditor-interop.js
let editors = {};

export function initializeEditor(editorId, placeholder) {
    return new Promise((resolve, reject) => {
        try {
            ClassicEditor
                .create(document.getElementById(editorId), {
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
                    language: 'vi',
                    licenseKey: '',
                    image: {
                        toolbar: [
                            'imageTextAlternative',
                            'toggleImageCaption',
                            'imageStyle:inline',
                            'imageStyle:block',
                            'imageStyle:side'
                        ]
                    }
                })
                .then(editor => {
                    editors[editorId] = editor;
                    console.log(`Editor ${editorId} initialized successfully`);
                    resolve(editor);
                })
                .catch(error => {
                    console.error(`Error initializing editor ${editorId}:`, error);
                    reject(error);
                });
        } catch (error) {
            console.error(`Exception initializing editor ${editorId}:`, error);
            reject(error);
        }
    });
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