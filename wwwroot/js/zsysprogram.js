// 2026-10-28 09:40 added: 系統程式資料維護 (Vue Composition API)
// 2026-10-28 10:30 modified: add create / edit / delete
// 2026-10-28 14:00 modified: master page only (detail moved to zsysprogram-det.js), remove codeNo
(function () {
    const { createApp, ref, onMounted } = Vue;

    const programHost = document.getElementById('program-app');
    const programToken = programHost.querySelector('input[name="__RequestVerificationToken"]').value; // 2026-10-28 10:30 added

    createApp({
        setup() {
            const masterList = ref([]);
            const masterLoading = ref(true);
            const error = ref('');

            // 2026-10-28 10:30 added: create / edit form
            const formModal = ref(null);     // the modal <div>
            const isNew = ref(true);         // true = create, false = edit
            const formTitle = ref('');
            const form = ref({});
            const formError = ref('');
            const busy = ref(false);

            // Read JSON from the server
            async function getJson(url) {
                const response = await fetch(url, { credentials: 'same-origin' });
                if (!response.ok) {
                    throw new Error('資料載入失敗，請稍後重試。');
                }
                return await response.json();
            }

            // 2026-10-28 10:30 added: POST to the server, return the JSON result
            async function postJson(url, data) {
                const response = await fetch(url, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        RequestVerificationToken: programToken
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

            // Master: top menus
            async function loadMaster() {
                masterLoading.value = true;
                error.value = '';
                try {
                    masterList.value = await getJson(programHost.dataset.masterUrl);
                } catch (requestError) {
                    error.value = requestError.message;
                } finally {
                    masterLoading.value = false;
                }
            }

            // 2026-10-28 14:00 added: link of the SubMenu button (ListDet?parentId=S01)
            function detPageUrl(item) {
                return programHost.dataset.detPageUrl + '?parentId=' + encodeURIComponent(item.formId);
            }

            // 2026-10-28 10:30 added: show / hide the Bootstrap modal
            function showModal() {
                bootstrap.Modal.getOrCreateInstance(formModal.value).show();
            }

            function hideModal() {
                bootstrap.Modal.getOrCreateInstance(formModal.value).hide();
            }

            // 2026-10-28 10:30 added: open an empty form
            // 2026-10-28 14:00 modified: master only, remove codeNo
            function openCreate() {
                isNew.value = true;
                formTitle.value = '新增主選單';
                formError.value = '';

                form.value = {
                    rowId: 0,
                    formId: '',
                    formCaption: '',
                    parentId: '',   // master has no parent
                    sortNo: '',
                    controllerName: '',
                    actionName: '',
                    routeName: '',
                    isEnabled: 1,
                    remark: ''
                };
                showModal();
            }

            // 2026-10-28 10:30 added: open the form with one row
            // 2026-10-28 14:00 modified: master only, remove codeNo
            function openEdit(item) {
                isNew.value = false;
                formTitle.value = '修改主選單';
                formError.value = '';

                // Copy the row, so the table does not change before saving
                form.value = {
                    rowId: item.rowId,
                    formId: item.formId,
                    formCaption: item.formCaption,
                    parentId: '',
                    sortNo: item.sortNo,
                    controllerName: item.controllerName,
                    actionName: item.actionName,
                    routeName: item.routeName,
                    isEnabled: item.isEnabled,
                    remark: item.remark
                };
                showModal();
            }

            // 2026-10-28 10:30 added: save (create or edit)
            async function saveForm() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                formError.value = '';

                try {
                    const url = isNew.value ? programHost.dataset.createUrl : programHost.dataset.updateUrl;
                    await postJson(url, form.value);
                    hideModal();
                    await loadMaster();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            // 2026-10-28 10:30 added: delete one master (also deletes its children)
            async function deleteRow(item) {
                const text = '確定要刪除 ' + item.formId + ' ' + item.formCaption + ' ？\n底下的程式也會一起刪除！';
                if (!confirm(text)) {
                    return;
                }

                error.value = '';
                try {
                    const url = programHost.dataset.deleteUrl + '?rowId=' + item.rowId;
                    await postJson(url, {});
                    await loadMaster();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            onMounted(loadMaster);

            return {
                masterList, masterLoading, error,
                formModal, isNew, formTitle, form, formError, busy,
                detPageUrl, openCreate, openEdit, saveForm, deleteRow
            };
        }
    }).mount(programHost);
})();
