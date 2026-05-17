// ckeditor-interop.js
const editors = new Map();

function createBlazorUploadAdapter(loader, bridge) {
    return {
        async upload() {
            const file = await loader.file;
            if (!file) throw new Error('Không thể đọc file ảnh.');

            const bytes = new Uint8Array(await file.arrayBuffer());
            const url = await bridge.invokeMethodAsync(
                'UploadEditorImageAsync',
                Array.from(bytes),
                file.name,
                file.type || 'application/octet-stream'
            );

            if (!url) throw new Error('Tải ảnh lên thất bại.');
            return { default: url };
        },
        abort() { }
    };
}

function getCKEditorCore() {
    return window.CKEDITOR;
}

function getPremiumFeatures() {
    return window.CKEDITOR_PREMIUM_FEATURES;
}

function waitForScripts(timeoutMs = 10000) {
    return new Promise((resolve, reject) => {
        if (getCKEditorCore()?.ClassicEditor && getPremiumFeatures()) {
            resolve();
            return;
        }

        const start = Date.now();
        const timer = setInterval(() => {
            if (getCKEditorCore()?.ClassicEditor && getPremiumFeatures()) {
                clearInterval(timer);
                resolve();
                return;
            }

            if (Date.now() - start > timeoutMs) {
                clearInterval(timer);
                reject(new Error('CKEditor scripts not loaded in time.'));
            }
        }, 100);
    });
}

function isConstructiblePlugin(plugin) {
    return typeof plugin === 'function' && typeof plugin.prototype === 'object' && (
        typeof plugin.prototype.init === 'function' ||
        typeof plugin.prototype.afterInit === 'function' ||
        typeof plugin.prototype.destroy === 'function'
    );
}

function pickPlugins(source, names, label = 'plugin') {
    const missing = [];
    const rejected = [];
    const plugins = [];

    for (const name of names) {
        const plugin = source?.[name];
        if (!plugin) {
            missing.push(name);
            continue;
        }

        if (isConstructiblePlugin(plugin)) {
            plugins.push(plugin);
        } else {
            rejected.push(name);
        }
    }

    return {
        plugins,
        missing: missing.map(name => `${label}.${name}`),
        rejected: rejected.map(name => `${label}.${name}`)
    };
}

function createEditorConfig(placeholder, initialData, dotNetRef, licenseKey, containerIds) {
    const CKEDITOR = getCKEditorCore();
    const PREMIUM = getPremiumFeatures();

    const editorPluginNames = [
        'Essentials',
        'Paragraph',
        'Bold',
        'Italic',
        'Underline',
        'BlockQuote',
        'Heading',
        'Link',
        'List',
        'Indent',
        'IndentBlock',
        'ImageBlock',
        'ImageCaption',
        'ImageEditing',
        'ImageInsert',
        'ImageResize',
        'ImageStyle',
        'ImageToolbar',
        'ImageUpload',
        'Table',
        'TableToolbar',
        'MediaEmbed',
        'PasteFromOffice',
        'Autoformat',
        'AutoLink',
        'FindAndReplace',
        'TextTransformation',
        'Alignment',
        'Highlight',
        'HorizontalLine',
        'SpecialCharacters',
        'RemoveFormat',
        'WordCount',
        'Fullscreen'
    ];

    const collaborationChannelId = containerIds?.channelId;
    const enableCollaboration = Boolean(collaborationChannelId);

    const premiumPluginNames = [];

    const { plugins: plugins, missing: missingEditorPlugins, rejected: rejectedEditorPlugins } = pickPlugins(CKEDITOR, editorPluginNames, 'CKEDITOR');
    const { plugins: premiumPlugins, missing: missingPremiumPlugins, rejected: rejectedPremiumPlugins } = pickPlugins(PREMIUM, premiumPluginNames, 'CKEDITOR_PREMIUM_FEATURES');

    if (missingEditorPlugins.length || missingPremiumPlugins.length || rejectedEditorPlugins.length || rejectedPremiumPlugins.length) {
        console.warn('CKEditor plugins filtered', {
            missingEditorPlugins,
            missingPremiumPlugins,
            rejectedEditorPlugins,
            rejectedPremiumPlugins,
            channelId: collaborationChannelId || null
        });
    }

    const getEl = (id) => id ? document.getElementById(id) : null;

    const config = {
        toolbar: {
            items: [
                'undo',
                'redo',
                '|',
                'heading',
                '|',
                'bold',
                'italic',
                'underline',
                'link',
                'bulletedList',
                'numberedList',
                '|',
                'insertImage',
                'insertTable',
                'mediaEmbed',
                '|',
                'blockQuote',
                'codeBlock',
                'removeFormat',
                'findAndReplace',
                '|',
                'alignment',
                'outdent',
                'indent'
            ],
            shouldNotGroupWhenFull: false
        },
        plugins: [...plugins, ...premiumPlugins],
        licenseKey: licenseKey || '',
        placeholder: placeholder || 'Nhập nội dung...',
        initialData: initialData || '',
        fontSize: { options: [10, 12, 14, 'default', 18, 20, 22], supportAllValues: true },
        fontFamily: { supportAllValues: true },
        link: {
            addTargetToExternalLinks: true,
            defaultProtocol: 'https://'
        },
        heading: {
            options: [
                { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
                { model: 'heading1', view: 'h1', title: 'Heading 1', class: 'ck-heading_heading1' },
                { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
                { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' },
                { model: 'heading4', view: 'h4', title: 'Heading 4', class: 'ck-heading_heading4' },
                { model: 'heading5', view: 'h5', title: 'Heading 5', class: 'ck-heading_heading5' },
                { model: 'heading6', view: 'h6', title: 'Heading 6', class: 'ck-heading_heading6' }
            ]
        },
        image: {
            toolbar: [
                'toggleImageCaption',
                'imageTextAlternative',
                '|',
                'imageStyle:inline',
                'imageStyle:wrapText',
                'imageStyle:breakText',
                '|',
                'resizeImage'
            ],
            upload: { types: ['jpeg', 'png', 'gif', 'bmp', 'webp', 'tiff'] }
        },
        table: {
            contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells', 'tableProperties', 'tableCellProperties']
        }
    };

    if (containerIds) {
        if (containerIds.annotationsId) {
            config.sidebar = { container: getEl(containerIds.annotationsId) };
        }
        if (containerIds.wordCountId) {
            config.wordCount = { container: getEl(containerIds.wordCountId) };
        }
    }

    return config;
}

export async function initializeEditor(editorId, placeholder, initialData, dotNetRef, licenseKey, containerIds) {
    const element = document.getElementById(editorId);
    if (!element) throw new Error(`Không tìm thấy #${editorId}`);

    await waitForScripts();

    const CKEDITOR = getCKEditorCore();
    const ClassicEditor = CKEDITOR.ClassicEditor;

    if (editors.has(editorId)) {
        await editors.get(editorId).destroy();
        editors.delete(editorId);
    }

    const config = createEditorConfig(placeholder, initialData, dotNetRef, licenseKey, containerIds);
    const editor = await ClassicEditor.create(element, config);

    if (dotNetRef) {
        const fileRepository = editor.plugins.get('FileRepository');
        fileRepository.createUploadAdapter = loader => createBlazorUploadAdapter(loader, dotNetRef);
    }

    if (containerIds?.wordCountId) {
        const wordCountPlugin = editor.plugins.get('WordCount');
        const wcContainer = document.getElementById(containerIds.wordCountId);
        if (wcContainer) {
            wcContainer.appendChild(wordCountPlugin.wordCountContainer);
        }
    }

    editors.set(editorId, editor);
    return editor;
}

export function getEditorData(editorId) {
    const editor = editors.get(editorId);
    return editor ? editor.getData() : '';
}

export function setEditorData(editorId, data) {
    const editor = editors.get(editorId);
    if (editor) editor.setData(data || '');
}

export async function destroyEditor(editorId) {
    const editor = editors.get(editorId);
    if (editor) {
        await editor.destroy();
        editors.delete(editorId);
    }
}
