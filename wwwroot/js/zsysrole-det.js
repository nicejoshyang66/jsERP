// 2026-10-29 10:51 added: 角色資料維護 - detail page of one user, tabs Roles / RoleGroups (Vue Composition API)
// 2026-10-29 15:44 modified: 群組 button removed, the group of a role is a select in the role form
(function () {
    const { createApp, ref, onMounted } = Vue;

    const detHost = document.getElementById('role-det-app');
    const detToken = detHost.querySelector('input[name="__RequestVerificationToken"]').value;
    const urls = detHost.dataset;        // all data-xxx-url of the page
    const userId = detHost.dataset.userId; // from the url (?userId=JOSH)

    createApp({
        setup() {
            const user = ref(null);          // the user on top
            const activeTab = ref('roles');
            const error = ref('');

            // Roles tab
            const roleList = ref([]);
            // 2026-10-29 15:44 removed: roleGroups, currentRoleId (群組 button)

            // RoleGroups tab (also the options of the 所屬群組 select)
            const groupList = ref([]);

            // Modals (the <div class="modal">) and their forms
            const roleModal = ref(null);
            const groupModal = ref(null);
            const roleForm = ref({});
            const groupForm = ref({});
            const isNew = ref(true);         // true = create, false = edit
            const formError = ref('');
            const busy = ref(false);

            // ---------- Helpers ----------

            // Read JSON from the server
            async function getJson(url) {
                const response = await fetch(url, { credentials: 'same-origin' });

                let result = {};
                try {
                    result = await response.json();
                } catch (jsonError) {
                    result = {};
                }

                if (!response.ok) {
                    throw new Error(result.message || '資料載入失敗，請稍後重試。');
                }
                return result;
            }

            // POST to the server, return the JSON result
            async function postJson(url, data) {
                const response = await fetch(url, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        RequestVerificationToken: detToken
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

            function showModal(modalRef) {
                bootstrap.Modal.getOrCreateInstance(modalRef.value).show();
            }

            function hideModal(modalRef) {
                bootstrap.Modal.getOrCreateInstance(modalRef.value).hide();
            }

            // Change tab and load its data
            async function changeTab(tab) {
                activeTab.value = tab;
                if (tab === 'roles') {
                    await loadRoles();
                } else {
                    await loadGroups();
                }
            }

            // Load the user on top, then the first tab
            // 2026-10-29 15:44 modified: also load the groups (options of the 所屬群組 select)
            async function loadUser() {
                error.value = '';
                try {
                    user.value = await getJson(urls.userOneUrl + '?userId=' + encodeURIComponent(userId));
                    await loadRoles();
                    await loadGroups();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // ==================== Roles tab ====================

            async function loadRoles() {
                error.value = '';
                try {
                    roleList.value = await getJson(urls.userRolesUrl + '?userId=' + encodeURIComponent(userId));
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // 2026-10-29 15:44 modified: add groupId ('' = no group)
            function openRoleCreate() {
                isNew.value = true;
                formError.value = '';
                roleForm.value = { roleId: '', roleDesc: '', userId: user.value.userId, groupId: '' };
                showModal(roleModal);
            }

            // 2026-10-29 15:44 modified: add groupId ('' = no group)
            function openRoleEdit(item) {
                isNew.value = false;
                formError.value = '';
                // Copy the row, so the table does not change before saving
                roleForm.value = {
                    roleId: item.roleId,
                    roleDesc: item.roleDesc,
                    userId: user.value.userId,
                    groupId: item.groupId || ''
                };
                showModal(roleModal);
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
                    hideModal(roleModal);
                    await loadRoles();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            async function deleteRole(item) {
                if (!confirm('確定要刪除角色 ' + item.roleId + ' ？\n它也會從所有群組中移除！')) {
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

            // 2026-10-29 15:44 removed: openRoleGroups, saveRoleGroups (群組 button)

            // ==================== RoleGroups tab ====================

            async function loadGroups() {
                error.value = '';
                try {
                    groupList.value = await getJson(urls.groupListUrl + '?userId=' + encodeURIComponent(userId));
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            function openGroupCreate() {
                isNew.value = true;
                formError.value = '';
                groupForm.value = { groupId: '', groupDesc: '' };
                showModal(groupModal);
            }

            function openGroupEdit(item) {
                isNew.value = false;
                formError.value = '';
                groupForm.value = { groupId: item.groupId, groupDesc: item.groupDesc };
                showModal(groupModal);
            }

            async function saveGroup() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                formError.value = '';
                try {
                    const url = isNew.value ? urls.groupCreateUrl : urls.groupUpdateUrl;
                    await postJson(url, groupForm.value);
                    hideModal(groupModal);
                    await loadGroups();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            async function deleteGroup(item) {
                if (!confirm('確定要刪除群組 ' + item.groupId + ' ？\n群組內的角色和程式權限也會一起刪除！')) {
                    return;
                }
                error.value = '';
                try {
                    await postJson(urls.groupDeleteUrl + '?groupId=' + encodeURIComponent(item.groupId), {});
                    await loadGroups();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            onMounted(loadUser);

            return {
                user, activeTab, error, changeTab,
                listPageUrl: urls.listPageUrl,
                // Roles
                // 2026-10-29 15:44 modified: 群組 button removed
                roleList,
                openRoleCreate, openRoleEdit, saveRole, deleteRole,
                // RoleGroups
                groupList,
                openGroupCreate, openGroupEdit, saveGroup, deleteGroup,
                // Modals and forms
                roleModal, groupModal,
                roleForm, groupForm, isNew, formError, busy
            };
        }
    }).mount(detHost);
})();
