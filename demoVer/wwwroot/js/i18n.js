window.setCultureCookie = (culture) => {
    if (!culture) {
        return;
    }

    const expiration = new Date();
    expiration.setFullYear(expiration.getFullYear() + 1);
    const expires = `expires=${expiration.toUTCString()}`;

    document.cookie = `.AspNetCore.Culture=c=${culture}|uic=${culture};${expires};path=/`;
    document.cookie = `BlazorCulture=${culture};${expires};path=/`;
};
