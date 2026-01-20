// JavaScript helpers for file upload functionality

window.getFileAsBase64 = function (inputId) {
    const input = document.getElementById(inputId);
    if (!input || !input.files || input.files.length === 0) {
        return null;
    }

    const file = input.files[0];
    
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = function (e) {
            resolve(e.target.result);
        };
        reader.onerror = function (error) {
            reject(error);
        };
        reader.readAsDataURL(file);
    });
};

window.getFileName = function (inputId) {
    const input = document.getElementById(inputId);
    if (!input || !input.files || input.files.length === 0) {
        return null;
    }
    return input.files[0].name;
};

window.getFileSize = function (inputId) {
    const input = document.getElementById(inputId);
    if (!input || !input.files || input.files.length === 0) {
        return 0;
    }
    return input.files[0].size;
};
