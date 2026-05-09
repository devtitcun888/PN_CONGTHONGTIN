//window.getMousePosition = (e) => {
//    return { clientX: window.clientX, clientY: window.clientX };
//};

window.getScreenWidth = () => {
    return window.innerWidth;
};


function Print(){
        var divcontent1 = document.getElementById("PrintDiv").innerHTML;
        //var divcontent2 = document.getElementById("PrintDiv").innerHTML;
        //var applyHeader = document.querySelector("#PrintDiv p")
        var a = window.open('', '', 'height=600, width=800');
        a.document.write('<html><head></head><body>');
        var styles = document.head.querySelectorAll('style, link[rel="stylesheet"]');
        styles.forEach(function (style) {
            a.document.head.appendChild(style.cloneNode(true));
        });
        a.document.write(divcontent1);
        a.document.write('</body></html>');
        a.document.close();
        a.print();
};


window.downloadFileFromBase64 = (fileName, base64Data) => {
    const link = document.createElement('a');
    link.href = `data:application/octet-stream;base64,${base64Data}`;
    link.download = fileName;
    link.click();
};