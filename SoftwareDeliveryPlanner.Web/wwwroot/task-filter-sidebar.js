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
