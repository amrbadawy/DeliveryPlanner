window.CommandPalette = {
    _handler: null,
    init: function (dotnetRef) {
        this._handler = function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                dotnetRef.invokeMethodAsync('Toggle');
            }
        };
        document.addEventListener('keydown', this._handler);
    },
    focusInput: function (element) {
        if (element) {
            setTimeout(function () { element.focus(); }, 50);
        }
    },
    dispose: function () {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
    }
};
