// 2026-10-30 19:32 added: 角色群組設定 - left: role groups, right: roles in the selected group (Vue Composition API)
(function () {
    const { createApp, ref, onMounted } = Vue;

    const groupHost = document.getElementById('role-group-app');
    const groupToken = groupHost.querySelector('input[name="__RequestVerificationToken"]').value;
    const urls = groupHost.dataset; // all data-xxx-url of the page

    createApp({
        setup() {
            const error = ref('');

            // Left: role groups
            const groupList = ref([]);
            const selectedGroupId = ref('');  // '' = no group selected yet

            // Right: roles in the selected group
            const roleList = ref([]);
            const freeRoleList = ref([]);     // options of the ROLE_ID select

            // Modals (the <div class="modal">) and their forms
            const groupModal = ref(null);
            const roleModal = ref(null);
            const groupForm = ref({});
            const roleForm = ref({});
            const isNew = ref(true);          // true = create, false = edit
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
                        RequestVerificationToken: groupToken
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

            // ==================== Left: role groups ====================

            async function loadGroups() {
                error.value = '';
                try {
                    groupList.value = await getJson(urls.groupListUrl);
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // Click a row -> show its roles on the right
            async function selectGroup(item) {
                selectedGroupId.value = item.groupId;
                await loadRoles();
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
                if (!confirm('確定要刪除群組 ' + item.groupId + ' ？\n群組內的角色設定和程式權限也會一起刪除！')) {
                    return;
                }
                error.value = '';
                try {
                    await postJson(urls.groupDeleteUrl + '?groupId=' + encodeURIComponent(item.groupId), {});
                    // the deleted group was selected -> clear the right side
                    if (selectedGroupId.value === item.groupId) {
                        selectedGroupId.value = '';
                        roleList.value = [];
                    }
                    await loadGroups();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // ==================== Right: roles in the selected group ====================

            async function loadRoles() {
                error.value = '';
                try {
                    roleList.value = await getJson(urls.groupRoleListUrl + '?groupId=' + encodeURIComponent(selectedGroupId.value));
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // Load the options of the ROLE_ID select (roles without a group, plus keepRoleId)
            async function loadFreeRoles(keepRoleId) {
                freeRoleList.value = await getJson(urls.freeRoleListUrl + '?keepRoleId=' + encodeURIComponent(keepRoleId));
            }

            // The role chosen in the select -> show its ROLE_DESC / USER_NAME
            function selectedRole() {
                for (const r of freeRoleList.value) {
                    if (r.roleId === roleForm.value.roleId) {
                        return r;
                    }
                }
                return { roleDesc: '', userName: '' };
            }

            async function openRoleCreate() {
                isNew.value = true;
                formError.value = '';
                roleForm.value = { groupId: selectedGroupId.value, oldRoleId: '', roleId: '' };
                try {
                    await loadFreeRoles('');
                    showModal(roleModal);
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            async function openRoleEdit(item) {
                isNew.value = false;
                formError.value = '';
                roleForm.value = { groupId: selectedGroupId.value, oldRoleId: item.roleId, roleId: item.roleId };
                try {
                    await loadFreeRoles(item.roleId);
                    showModal(roleModal);
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            async function saveRole() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                formError.value = '';
                try {
                    const url = isNew.value ? urls.groupRoleCreateUrl : urls.groupRoleUpdateUrl;
                    await postJson(url, roleForm.value);
                    hideModal(roleModal);
                    await loadRoles();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            // Only removes the role from this group, the role itself is kept
            async function deleteRole(item) {
                if (!confirm('確定要把角色 ' + item.roleId + ' 從群組 ' + selectedGroupId.value + ' 移除？')) {
                    return;
                }
                error.value = '';
                try {
                    await postJson(urls.groupRoleDeleteUrl
                        + '?groupId=' + encodeURIComponent(selectedGroupId.value)
                        + '&roleId=' + encodeURIComponent(item.roleId), {});
                    await loadRoles();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            onMounted(loadGroups);

            return {
                error,
                // Left
                groupList, selectedGroupId, selectGroup,
                openGroupCreate, openGroupEdit, saveGroup, deleteGroup,
                // Right
                roleList, freeRoleList, selectedRole,
                openRoleCreate, openRoleEdit, saveRole, deleteRole,
                // Modals and forms
                groupModal, roleModal, groupForm, roleForm, isNew, formError, busy
            };
        }
    }).mount(groupHost);
})();
