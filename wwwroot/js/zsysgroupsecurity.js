// 2026-10-30 11:13 added: 群組權限資料維護 - rights of one role group (Vue Composition API)
(function () {
    const { createApp, ref, onMounted } = Vue;

    const host = document.getElementById('group-security-app');
    const token = host.querySelector('input[name="__RequestVerificationToken"]').value;
    const urls = host.dataset;   // all data-xxx-url of the page

    createApp({
        setup() {
            const groupList = ref([]);     // options of the 群組代號 select
            const groupId = ref('');       // the selected group
            const programList = ref([]);   // rights of the selected group
            const message = ref('');
            const error = ref('');
            const busy = ref(false);

            // Copy modal
            const copyModal = ref(null);
            const sourceId = ref('');
            const formError = ref('');

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
                        RequestVerificationToken: token
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

            // Name of the selected group
            function groupDesc() {
                const group = groupList.value.find(function (g) {
                    return g.groupId === groupId.value;
                });
                return group ? group.groupDesc : '';
            }

            // Groups of the 來源群組 select (all groups except the selected one)
            function sourceGroups() {
                return groupList.value.filter(function (g) {
                    return g.groupId !== groupId.value;
                });
            }

            // ---------- Load ----------

            // Load the groups, then select the first one
            async function loadGroups() {
                error.value = '';
                try {
                    groupList.value = await getJson(urls.groupListUrl);
                    if (groupList.value.length > 0) {
                        groupId.value = groupList.value[0].groupId;
                        await loadPrograms();
                    }
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // Load the rights of the selected group (also used by the select)
            async function loadPrograms() {
                message.value = '';
                error.value = '';
                try {
                    programList.value = await getJson(urls.programListUrl + '?groupId=' + encodeURIComponent(groupId.value));
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            // ---------- Buttons ----------

            async function save() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                message.value = '';
                error.value = '';
                try {
                    const result = await postJson(urls.saveUrl, {
                        groupId: groupId.value,
                        rows: programList.value
                    });
                    await loadPrograms();
                    message.value = result.message;
                } catch (requestError) {
                    error.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            // 取消 = load the data again (changes are not saved)
            async function cancel() {
                await loadPrograms();
            }

            function openCopy() {
                sourceId.value = '';
                formError.value = '';
                bootstrap.Modal.getOrCreateInstance(copyModal.value).show();
            }

            async function copy() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                formError.value = '';
                try {
                    const result = await postJson(urls.copyUrl, {
                        targetId: groupId.value,
                        sourceId: sourceId.value
                    });
                    bootstrap.Modal.getOrCreateInstance(copyModal.value).hide();
                    await loadPrograms();
                    message.value = result.message;
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            onMounted(loadGroups);

            return {
                groupList, groupId, programList, message, error, busy,
                groupDesc, loadPrograms, save, cancel,
                // Copy modal
                copyModal, sourceId, formError, sourceGroups, openCopy, copy
            };
        }
    }).mount(host);
})();
