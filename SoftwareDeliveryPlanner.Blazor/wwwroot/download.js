window.BlazorDownloadFile = function (fileName, content, contentType) {
    window.__lastDownloadCall = {
        fileName: fileName,
        contentType: contentType,
        contentLength: content ? content.length : 0,
        at: new Date().toISOString()
    };

    var blob = new Blob([content], { type: contentType });
    var url = window.URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};
