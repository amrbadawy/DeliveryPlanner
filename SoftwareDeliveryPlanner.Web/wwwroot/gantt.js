/* ──────────────────────────────────────────────────────────
   Gantt Zoom — JS interop helpers
   Provides:
     - GanttZoom.preserveAnchor(container, beforeWidth, afterWidth)
         keeps the horizontally-centered date stable across zoom changes
     - GanttZoom.scrollToToday(container, todayLeftPx, labelColPx)
         scrolls so today's marker is centered in the viewport
     - GanttZoom.scrollToFit(container)
         resets scroll to start (Fit-to-screen)
     - GanttZoom.measureViewport(container)
         returns { scrollLeft, clientWidth, scrollWidth } for the timeline area
     - GanttZoom.bindKeyboard(dotnetRef)
         registers global shortcuts: 1/2/3/4 -> zoom, F -> fit, T -> today
     - GanttZoom.unbindKeyboard()
   ────────────────────────────────────────────────────────── */
window.GanttZoom = {
    _kbHandler: null,

    measureViewport: function (container) {
        if (!container) return { scrollLeft: 0, clientWidth: 0, scrollWidth: 0 };
        return {
            scrollLeft: container.scrollLeft || 0,
            clientWidth: container.clientWidth || 0,
            scrollWidth: container.scrollWidth || 0
        };
    },

    /**
     * Preserve the horizontally-centered date anchor across a zoom change.
     * Caller passes the timeline-pixel widths before and after the zoom switch.
     * The label column (sticky left) is excluded from the scrollable timeline area.
     */
    preserveAnchor: function (container, beforeWidth, afterWidth, labelColPx) {
        if (!container || beforeWidth <= 0 || afterWidth <= 0) return;
        var labelCol = labelColPx || 240;
        var viewport = container.clientWidth - labelCol;
        var centerBefore = container.scrollLeft + viewport / 2;
        // Ratio in [0,1] of the timeline area
        var ratio = (centerBefore - 0) / beforeWidth;
        if (ratio < 0) ratio = 0;
        if (ratio > 1) ratio = 1;
        var newCenter = ratio * afterWidth;
        var newScroll = Math.max(0, newCenter - viewport / 2);
        container.scrollLeft = newScroll;
    },

    scrollToToday: function (container, todayLeftPx, labelColPx) {
        if (!container) return;
        var labelCol = labelColPx || 240;
        var viewport = container.clientWidth - labelCol;
        var target = Math.max(0, todayLeftPx - viewport / 2);
        container.scrollLeft = target;
    },

    scrollToFit: function (container) {
        if (!container) return;
        container.scrollLeft = 0;
    },

    bindKeyboard: function (dotnetRef) {
        var self = this;
        if (this._kbHandler) {
            document.removeEventListener('keydown', this._kbHandler);
        }
        this._kbHandler = function (e) {
            // Ignore when typing in an input/textarea/contenteditable
            var t = e.target;
            if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) {
                return;
            }
            // Ignore modifier-bound shortcuts (let CommandPalette handle Ctrl+K, etc.)
            if (e.ctrlKey || e.metaKey || e.altKey) return;

            switch (e.key) {
                case '1': dotnetRef.invokeMethodAsync('OnZoomKey', 'DAY'); e.preventDefault(); break;
                case '2': dotnetRef.invokeMethodAsync('OnZoomKey', 'WEEK'); e.preventDefault(); break;
                case '3': dotnetRef.invokeMethodAsync('OnZoomKey', 'MONTH'); e.preventDefault(); break;
                case '4': dotnetRef.invokeMethodAsync('OnZoomKey', 'QUARTER'); e.preventDefault(); break;
                case 'f':
                case 'F': dotnetRef.invokeMethodAsync('OnZoomKey', 'FIT'); e.preventDefault(); break;
                case 't':
                case 'T': dotnetRef.invokeMethodAsync('OnZoomKey', 'TODAY'); e.preventDefault(); break;
            }
        };
        document.addEventListener('keydown', this._kbHandler);
    },

    unbindKeyboard: function () {
        if (this._kbHandler) {
            document.removeEventListener('keydown', this._kbHandler);
            this._kbHandler = null;
        }
    }
};
