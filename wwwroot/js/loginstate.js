const originalLocalStorageGetItem = window.localStorage.getItem;
const originalLocalStorageSetItem = window.localStorage.setItem;
const originalLocalStorageRemoveItem = window.localStorage.removeItem;

window.localStorage.getItem = function (key) {
    return originalLocalStorageGetItem.call(localStorage, key);
};

window.localStorage.setItem = function (key, value) {
    originalLocalStorageSetItem.call(localStorage, key, value);
};

window.localStorage.removeItem = function (key) {
    originalLocalStorageRemoveItem.call(localStorage, key);
};