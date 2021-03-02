export function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}
export function getContainerLocation(containerId) {
    let e = document.querySelector(`[_bl_${containerId}=\"\"]`);
    let bnd = e.getBoundingClientRect();
    let out = { 'X': bnd.x, 'Y': bnd.y };
    console.log(JSON.stringify(out));
    return out;
}
export function getWidowDimentions() {
    let output = window.innerWidth;
    console.log('window width ' + output);
    return output;
}
