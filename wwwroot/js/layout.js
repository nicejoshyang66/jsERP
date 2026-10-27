// 2026-10-24 17:35 added: top bar + left menu (Vue Composition API)
const { createApp, ref, onMounted } = Vue;

const layoutHost = document.getElementById('layout-app');
const layoutToken = layoutHost.querySelector('input[name="__RequestVerificationToken"]').value;

createApp({
    setup() {
        const userId = ref('');
        const userName = ref('');
        const menu = ref([]);
        const opened = ref([]);     // ids of opened top menus
        const loading = ref(true);
        const error = ref('');
        const busy = ref(false);

        // Show / hide the left menu.
        // Desktop: menu is shown by default, click to hide.
        // Phone:   menu is hidden by default, click to show.
        function toggleSidebar() {
            document.body.classList.toggle('sidebar-toggled');
        }

        function isOpen(id) {
            return opened.value.includes(id);
        }

        function toggleMenu(id) {
            const index = opened.value.indexOf(id);
            if (index === -1) {
                opened.value.push(id);        // open
            } else {
                opened.value.splice(index, 1); // close
            }
        }

        // Is this link the current page?
        function isActive(url) {
            if (!url || url === '#') {
                return false;
            }
            const target = new URL(url, location.origin);
            return target.pathname.toLowerCase() === location.pathname.toLowerCase()
                && target.search === location.search;
        }

        async function loadMenu() {
            loading.value = true;
            error.value = '';

            try {
                const response = await fetch(layoutHost.dataset.menuUrl, { credentials: 'same-origin' });

                // Not logged in -> back to login page
                if (response.status === 401) {
                    location.assign(layoutHost.dataset.loginUrl);
                    return;
                }
                if (!response.ok) {
                    throw new Error('選單載入失敗，請稍後重試。');
                }

                const result = await response.json();
                userId.value = result.userId || '';
                userName.value = result.userName || '';
                menu.value = result.menu;

                // Open the top menu that contains the current page
                opened.value = [];
                for (const parent of menu.value) {
                    for (const child of parent.children) {
                        if (isActive(child.url)) {
                            opened.value.push(parent.id);
                        }
                    }
                }
            } catch (requestError) {
                error.value = requestError.message;
            } finally {
                loading.value = false;
            }
        }

        async function logout() {
            if (busy.value) {
                return;
            }
            busy.value = true;

            try {
                const response = await fetch(layoutHost.dataset.logoutUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { RequestVerificationToken: layoutToken }
                });
                if (!response.ok) {
                    throw new Error('登出失敗，請重新整理後再試。');
                }
                const result = await response.json();
                location.assign(result.redirectUrl);
            } catch (requestError) {
                alert(requestError.message);
            } finally {
                busy.value = false;
            }
        }

        onMounted(loadMenu);

        return {
            userId, userName, menu, loading, error, busy,
            toggleSidebar, isOpen, toggleMenu, isActive, loadMenu, logout
        };
    }
}).mount(layoutHost);
