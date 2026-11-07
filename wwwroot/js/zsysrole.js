// 2026-10-28 15:00 added: 角色資料維護 - Users / Roles / Role Groups (Vue Composition API)
// 2026-10-29 10:51 modified: this page only shows users (add / edit / delete), 設定群組 goes to ListDet
// 2026-10-30 19:32 modified: 2 tabs - Users / Roles. 設定群組 button and ListDet page removed
(function () {
    const { createApp, ref, onMounted } = Vue;

    const roleHost = document.getElementById('role-app');
    const roleToken = roleHost.querySelector('input[name="__RequestVerificationToken"]').value;
    const urls = roleHost.dataset; // all data-xxx-url of the page

    createApp({
        setup() {
            const userList = ref([]);
            const loading = ref(true);
            const error = ref('');
            const activeTab = ref('users');  // 2026-10-30 19:32 added: 'users' or 'roles'

            // 2026-10-30 19:32 added: Roles tab
            const roleList = ref([]);
            const roleModal = ref(null);
            const roleForm = ref({});

            // Modal (the <div class="modal">) and its form
            const userModal = ref(null);
            const userForm = ref({});
            const isNew = ref(true);        // true = create, false = edit
            const formError = ref('');
            const busy = ref(false);

            // 2026-10-29 13:13 added: change password modal
            const passwordModal = ref(null);
            const passwordForm = ref({});
            const passwordError = ref('');

            // ---------- Helpers ----------

            // Read JSON from the server
            async function getJson(url) {
                const response = await fetch(url, { credentials: 'same-origin' });
                if (!response.ok) {
                    throw new Error('資料載入失敗，請稍後重試。');
                }
                return await response.json();
            }

            // POST to the server, return the JSON result
            async function postJson(url, data) {
                const response = await fetch(url, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        RequestVerificationToken: roleToken
                    },
                    body: JSON.stringify(data)
                });

                let result = {};
                try {
                    result = await response.json();
                } catch (jsonError) {
                    result = {};
                }

                if (!response.ok) {
                    throw new Error(result.message || '儲存失敗，請稍後重試。');
                }
                return result;
            }

            function showModal() {
                bootstrap.Modal.getOrCreateInstance(userModal.value).show();
            }

            function hideModal() {
                bootstrap.Modal.getOrCreateInstance(userModal.value).hide();
            }

            // 2026-10-30 19:32 removed: detPageUrl (設定群組 button is removed)

            // 2026-10-30 19:32 added: change tab and load its data
            async function changeTab(tab) {
                activeTab.value = tab;
                if (tab === 'users') {
                    await loadUsers();
                } else {
                    await loadRoles();
                }
            }

            // ---------- Users ----------

            async function loadUsers() {
                error.value = '';
                try {
                    userList.value = await getJson(urls.userListUrl);
                } catch (requestError) {
                    error.value = requestError.message;
                } finally {
                    loading.value = false;
                }
            }

            // 2026-10-29 10:51 modified: add tel, mobile
            // 2026-10-29 13:13 modified: no password / autoLogin; new user: password = userId, isActive = 1 (server side)
            function openCreate() {
                isNew.value = true;
                formError.value = '';
                userForm.value = {
                    userId: '', userName: '', email: '',
                    description: '', tel: '', mobile: '', isActive: 1
                };
                showModal();
            }

            // 2026-10-29 10:51 modified: the row is passed from the Edit button; add tel, mobile
            // 2026-10-29 13:13 modified: no password / autoLogin; add isActive
            function openEdit(item) {
                isNew.value = false;
                formError.value = '';
                // Copy the row, so the table does not change before saving
                userForm.value = {
                    userId: item.userId,
                    userName: item.userName,
                    email: item.email,
                    description: item.description,
                    tel: item.tel,
                    mobile: item.mobile,
                    isActive: item.isActive === 1 ? 1 : 0
                };
                showModal();
            }

            // 2026-10-29 13:13 added: lock button -> change password modal
            function openPassword(item) {
                passwordError.value = '';
                passwordForm.value = {
                    userId: item.userId,
                    userName: item.userName,
                    newPassword: '',
                    confirmPassword: ''
                };
                bootstrap.Modal.getOrCreateInstance(passwordModal.value).show();
            }

            // 2026-10-29 13:13 added: save the new password
            async function savePassword() {
                if (busy.value) {
                    return;
                }
                if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
                    passwordError.value = '兩次輸入的密碼不一樣。';
                    return;
                }
                busy.value = true;
                passwordError.value = '';
                try {
                    const result = await postJson(urls.userPasswordUrl, passwordForm.value);
                    bootstrap.Modal.getOrCreateInstance(passwordModal.value).hide();
                    alert(result.message || '密碼已變更。');
                } catch (requestError) {
                    passwordError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            async function saveUser() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                formError.value = '';
                try {
                    const url = isNew.value ? urls.userCreateUrl : urls.userUpdateUrl;
                    await postJson(url, userForm.value);
                    hideModal();
                    await loadUsers();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            // 2026-10-29 10:51 modified: the row is passed from the Delete button
            async function deleteUser(item) {
                if (!confirm('確定要刪除使用者 ' + item.userId + ' ' + (item.userName || '') + ' ？')) {
                    return;
                }
                error.value = '';
                try {
                    await postJson(urls.userDeleteUrl + '?userId=' + encodeURIComponent(item.userId), {});
                    await loadUsers();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // ==================== Roles tab (2026-10-30 19:32 added) ====================

            // All roles + user name. Also load users (options of the 使用者 select).
            async function loadRoles() {
                error.value = '';
                try {
                    roleList.value = await getJson(urls.roleListUrl);
                    userList.value = await getJson(urls.userListUrl);
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            function openRoleCreate() {
                isNew.value = true;
                formError.value = '';
                roleForm.value = { roleId: '', roleDesc: '', userId: '' };
                bootstrap.Modal.getOrCreateInstance(roleModal.value).show();
            }

            function openRoleEdit(item) {
                isNew.value = false;
                formError.value = '';
                // Copy the row, so the table does not change before saving
                roleForm.value = {
                    roleId: item.roleId,
                    roleDesc: item.roleDesc,
                    userId: item.userId || ''
                };
                bootstrap.Modal.getOrCreateInstance(roleModal.value).show();
            }

            async function saveRole() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                formError.value = '';
                try {
                    const url = isNew.value ? urls.roleCreateUrl : urls.roleUpdateUrl;
                    await postJson(url, roleForm.value);
                    bootstrap.Modal.getOrCreateInstance(roleModal.value).hide();
                    await loadRoles();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            async function deleteRole(item) {
                if (!confirm('確定要刪除角色 ' + item.roleId + ' ？\n它也會從所屬群組中移除！')) {
                    return;
                }
                error.value = '';
                try {
                    await postJson(urls.roleDeleteUrl + '?roleId=' + encodeURIComponent(item.roleId), {});
                    await loadRoles();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            onMounted(loadUsers);

            return {
                userList, loading, error,
                activeTab, changeTab,  // 2026-10-30 19:32 added; detPageUrl removed
                openCreate, openEdit, saveUser, deleteUser,
                userModal, userForm, isNew, formError, busy,
                // 2026-10-29 13:13 added: change password
                passwordModal, passwordForm, passwordError, openPassword, savePassword,
                // 2026-10-30 19:32 added: Roles tab
                roleList, roleModal, roleForm,
                openRoleCreate, openRoleEdit, saveRole, deleteRole
            };
        }
    }).mount(roleHost);
})();
