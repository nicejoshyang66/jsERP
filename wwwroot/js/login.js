// 2026-10-24 17:35 added: login page (Vue Composition API)
const { createApp, ref } = Vue;

const loginHost = document.getElementById('login-app');
const loginToken = loginHost.querySelector('input[name="__RequestVerificationToken"]').value;

createApp({
    setup() {
        const userId = ref('');
        const password = ref('');
        const rememberMe = ref(false);
        const busy = ref(false);
        const error = ref('');

        async function submitLogin() {
            // Do not send twice
            if (busy.value) {
                return;
            }

            busy.value = true;
            error.value = '';

            try {
                // 1. Send user id + password to the server
                const response = await fetch(loginHost.dataset.loginUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        RequestVerificationToken: loginToken
                    },
                    body: JSON.stringify({
                        userId: userId.value,
                        password: password.value,
                        rememberMe: rememberMe.value
                    })
                });

                // 2. Read the JSON result (empty object if not JSON)
                let result = {};
                try {
                    result = await response.json();
                } catch (jsonError) {
                    result = {};
                }

                if (!response.ok) {
                    throw new Error(result.message || '登入失敗，請重新整理頁面再試。');
                }

                // 3. Success -> go to home page
                window.location.assign(result.redirectUrl);
            } catch (requestError) {
                error.value = requestError.message || '無法連線，請稍後再試。';
            } finally {
                busy.value = false;
            }
        }

        return { userId, password, rememberMe, busy, error, submitLogin };
    }
}).mount(loginHost);
