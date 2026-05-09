/**
 * secureStorage.js
 * Bảo mật localStorage bằng cách mã hóa XOR với key chung cho tất cả tab.
 *
 * === VẤN ĐỀ MỞ TAB MỚI ===
 * Vấn đề: Dùng sessionStorage cho encryption key → mỗi tab có key riêng
 *          → Tab mới không đọc được session của tab cũ → bắt đăng nhập lại.
 *
 * Giải pháp: Dùng localStorage cho encryption key (các tab trong cùng browser chia sẻ)
 *   + Key được xóa khi đăng xuất → dữ liệu cũ không đọc được sau logout
 *   + SharedTabId là key CHUNG: tất cả tab cùng đọc từ một session duy nhất
 *
 * === THIẾT KẾ ĐA TAB ===
 *   localStorage["_pn_sk"]             = encryption key (chung cho tất cả tab)
 *   localStorage["SharedTabId"]        = "tab_abc123"  (chung cho tất cả tab)
 *   localStorage["UserSession_tab_abc123"] = "PN_SEC:..." (session mã hóa)
 *
 * Mở tab mới → đọc SharedTabId → đọc UserSession_tab_xxx → giải mã bằng _pn_sk → ✅ OK
 *
 * === NHẬN BIẾT DATA MÃ HÓA ===
 * Dùng prefix "PN_SEC:" (100% đáng tin cậy, không bị nhầm với plain string như "2024-2025")
 *   - Data MÃ HÓA:      "PN_SEC:YHj8xKL2..."
 *   - Data CŨ/PLAIN:    '{"MaUser":"abc"}' hoặc "2024-2025" → đọc nguyên (backward compat)
 */

window.secureStorage = (function () {
    const ENC_KEY_NAME     = '_pn_sk';    // Encryption key lưu trong localStorage (chung tất cả tab)
    const KEY_BYTE_LENGTH  = 32;          // 256-bit key
    const ENCRYPTED_PREFIX = 'PN_SEC:';  // Prefix nhận biết data đã mã hóa

    // ─── PRIVATE: Key Management ───────────────────────────────────────────────

    /**
     * Lấy hoặc tạo encryption key dùng chung cho tất cả tab trong cùng browser.
     * Lưu trong localStorage (không phải sessionStorage) để cross-tab hoạt động.
     * Key chỉ bị xóa khi người dùng đăng xuất (clearAllSessions).
     */
    function _getOrCreateEncKey() {
        let key = localStorage.getItem(ENC_KEY_NAME);
        if (!key) {
            key = _generateKey();
            localStorage.setItem(ENC_KEY_NAME, key);
        }
        return key;
    }

    function _generateKey() {
        if (window.crypto && window.crypto.getRandomValues) {
            const arr = new Uint8Array(KEY_BYTE_LENGTH);
            window.crypto.getRandomValues(arr);
            return Array.from(arr).map(b => b.toString(16).padStart(2, '0')).join('');
        }
        return Array.from({ length: KEY_BYTE_LENGTH },
            () => Math.floor(Math.random() * 256).toString(16).padStart(2, '0')).join('');
    }

    // ─── PRIVATE: Encoding ─────────────────────────────────────────────────────

    function _toByteArray(str) {
        return new TextEncoder().encode(str);
    }

    function _fromByteArray(bytes) {
        return new TextDecoder().decode(bytes);
    }

    function _xor(data, hexKey) {
        const keyBytes = hexKey.match(/.{1,2}/g).map(b => parseInt(b, 16));
        const result   = new Uint8Array(data.length);
        for (let i = 0; i < data.length; i++) {
            result[i] = data[i] ^ keyBytes[i % keyBytes.length];
        }
        return result;
    }

    // ─── PRIVATE: Encrypt / Decrypt ────────────────────────────────────────────

    /** Mã hóa: string → XOR → Base64 → thêm prefix "PN_SEC:" */
    function _encrypt(plaintext) {
        try {
            const key       = _getOrCreateEncKey();
            const bytes     = _toByteArray(plaintext);
            const encrypted = _xor(bytes, key);
            let binary = '';
            encrypted.forEach(b => { binary += String.fromCharCode(b); });
            return ENCRYPTED_PREFIX + btoa(binary);
        } catch (e) {
            console.warn('[secureStorage] Encrypt error:', e);
            return null;
        }
    }

    /** Giải mã: bỏ prefix → Base64 decode → XOR → string */
    function _decrypt(ciphertext) {
        try {
            const base64   = ciphertext.startsWith(ENCRYPTED_PREFIX)
                ? ciphertext.substring(ENCRYPTED_PREFIX.length)
                : ciphertext;
            const key      = _getOrCreateEncKey();
            const binary   = atob(base64);
            const bytes    = Uint8Array.from(binary, c => c.charCodeAt(0));
            const decrypted = _xor(bytes, key);
            return _fromByteArray(decrypted);
        } catch (e) {
            console.warn('[secureStorage] Decrypt error:', e);
            return null;
        }
    }

    // ─── PRIVATE: TabId ────────────────────────────────────────────────────────

    function _createNewTabId() {
        if (window.crypto && window.crypto.getRandomValues) {
            const arr = new Uint8Array(8);
            window.crypto.getRandomValues(arr);
            return 'tab_' + Array.from(arr).map(b => b.toString(16).padStart(2, '0')).join('');
        }
        return 'tab_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    // ─── PUBLIC API ────────────────────────────────────────────────────────────

    return {

        /**
         * Lưu giá trị vào localStorage với mã hóa.
         * Tất cả tab dùng cùng một encryption key → cross-tab hoạt động bình thường.
         */
        setItem: function (key, value) {
            try {
                const encrypted = _encrypt(value);
                localStorage.setItem(key, encrypted !== null ? encrypted : value);
                if (encrypted === null) {
                    console.warn('[secureStorage] Stored plaintext (encrypt failed) for key:', key);
                }
            } catch (e) {
                console.error('[secureStorage] setItem error:', e);
            }
        },

        /**
         * Đọc và giải mã giá trị từ localStorage.
         * Backward compatible: dữ liệu cũ không có prefix "PN_SEC:" → trả về nguyên.
         */
        getItem: function (key) {
            try {
                const stored = localStorage.getItem(key);
                if (stored === null) return null;

                if (stored.startsWith(ENCRYPTED_PREFIX)) {
                    // Dữ liệu mới → giải mã
                    return _decrypt(stored);
                } else {
                    // Dữ liệu cũ chưa mã hóa (plain JSON, plain string "2024-2025", v.v.)
                    // → Trả về nguyên, lần sau ghi lại sẽ tự động mã hóa
                    return stored;
                }
            } catch (e) {
                console.error('[secureStorage] getItem error:', e);
                return null;
            }
        },

        /** Xóa một item khỏi localStorage. */
        removeItem: function (key) {
            try {
                localStorage.removeItem(key);
            } catch (e) {
                console.error('[secureStorage] removeItem error:', e);
            }
        },

        /**
         * Đăng xuất: xóa tất cả session keys VÀ encryption key.
         * Sau khi gọi hàm này, dữ liệu cũ trong localStorage không thể giải mã được
         * (vì key đã bị xóa và lần đăng nhập mới sẽ sinh key hoàn toàn khác).
         */
        clearAllSessions: function () {
            try {
                // Xóa tất cả UserSession_*
                const keysToRemove = [];
                for (let i = 0; i < localStorage.length; i++) {
                    const k = localStorage.key(i);
                    if (k && k.startsWith('UserSession_')) {
                        keysToRemove.push(k);
                    }
                }
                keysToRemove.forEach(k => localStorage.removeItem(k));

                // Xóa encryption key → dữ liệu cũ (nếu còn) không thể giải mã
                localStorage.removeItem(ENC_KEY_NAME);

                // Xóa SharedTabId để lần sau login tạo mới
                localStorage.removeItem('SharedTabId');

                console.info('[secureStorage] All sessions cleared. Encryption key destroyed.');
            } catch (e) {
                console.error('[secureStorage] clearAllSessions error:', e);
            }
        },

        /**
         * Lấy SharedTabId hiện tại (chung cho tất cả tab).
         * Nếu chưa có → tạo mới và lưu vào localStorage.
         */
        getOrCreateSharedTabId: function () {
            try {
                let tabId = localStorage.getItem('SharedTabId');
                if (!tabId) {
                    tabId = _createNewTabId();
                    localStorage.setItem('SharedTabId', tabId);
                    console.info('[secureStorage] Created new SharedTabId:', tabId);
                }
                return tabId;
            } catch (e) {
                return 'tab_fallback_' + Date.now();
            }
        },

        /**
         * Tạo TabId MỚI sau khi đăng nhập (chống Session Fixation Attack).
         * Xóa session cũ của TabId trước, tạo TabId mới, lưu vào localStorage.
         * Tất cả tab sau đó sẽ dùng TabId mới này.
         */
        regenerateTabId: function () {
            try {
                const oldTabId = localStorage.getItem('SharedTabId');
                if (oldTabId) {
                    localStorage.removeItem('UserSession_' + oldTabId);
                }
                const newTabId = _createNewTabId();
                localStorage.setItem('SharedTabId', newTabId);
                console.info('[secureStorage] TabId regenerated:', oldTabId, '→', newTabId);
                return newTabId;
            } catch (e) {
                return 'tab_' + Date.now();
            }
        },

        /** Lấy User-Agent string để tạo session fingerprint. */
        getUserAgentInfo: function () {
            return navigator.userAgent || '';
        }
    };

})();
