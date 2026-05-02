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

/**
 * Roving-tabindex keyboard navigation for chip groups.
 * Delegates ArrowLeft/ArrowRight/Home/End handling to the document so it
 * survives Blazor re-renders. Each [role="group"].task-filter-chip-group
 * gets one focusable chip at a time (the active or first chip).
 */
window.TaskFilterSidebarRoving = {
    _handler: null,

    bind: function () {
        this.unbind();
        this._handler = function (e) {
            var key = e.key;
            if (key !== 'ArrowLeft' && key !== 'ArrowRight' && key !== 'Home' && key !== 'End') return;

            var t = e.target;
            if (!t || !t.classList || !t.classList.contains('task-filter-chip')) return;

            var group = t.closest('.task-filter-chip-group');
            if (!group) return;

            var chips = Array.prototype.slice.call(group.querySelectorAll('.task-filter-chip'));
            if (chips.length === 0) return;
            var idx = chips.indexOf(t);
            var next = idx;

            if (key === 'ArrowLeft') next = (idx - 1 + chips.length) % chips.length;
            else if (key === 'ArrowRight') next = (idx + 1) % chips.length;
            else if (key === 'Home') next = 0;
            else if (key === 'End') next = chips.length - 1;

            if (next !== idx) {
                chips.forEach(function (c) { c.setAttribute('tabindex', '-1'); });
                chips[next].setAttribute('tabindex', '0');
                chips[next].focus();
                e.preventDefault();
            }
        };
        document.addEventListener('keydown', this._handler);
    },

    unbind: function () {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
    }
};
