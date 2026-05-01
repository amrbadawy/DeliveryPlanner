window.TaskFilterSidebarKeyboard = {
    _handler: null,

    bind: function (dotnetRef) {
        this.unbind();

        this._handler = function (e) {
            var t = e.target;
            var isEditable = !!(t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable));

            if (e.ctrlKey || e.metaKey || e.altKey) return;

            if (!isEditable && e.key === '\\') {
                dotnetRef.invokeMethodAsync('OnSidebarShortcut', 'TOGGLE');
                e.preventDefault();
                return;
            }

            if (!isEditable && e.key === '/') {
                dotnetRef.invokeMethodAsync('OnSidebarShortcut', 'FOCUS');
                e.preventDefault();
            }
        };

        document.addEventListener('keydown', this._handler);
    },

    focusSearch: function () {
        var el = document.getElementById('task-filter-search');
        if (el) {
            el.focus();
            if (typeof el.select === 'function') el.select();
        }
    },

    unbind: function () {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
    }
};

window.TaskFilterSidebarClipboard = {
    copyText: async function (text) {
        if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(text);
            return;
        }

        var ta = document.createElement('textarea');
        ta.value = text;
        ta.setAttribute('readonly', 'readonly');
        ta.style.position = 'fixed';
        ta.style.left = '-9999px';
        document.body.appendChild(ta);
        ta.select();
        document.execCommand('copy');
        document.body.removeChild(ta);
    }
};

window.TaskFilterSidebarStorage = {
    getCollapsed: function (key) {
        try { return localStorage.getItem(key) === 'true'; } catch { return false; }
    },
    setCollapsed: function (key, value) {
        try { localStorage.setItem(key, value ? 'true' : 'false'); } catch { }
    }
};
