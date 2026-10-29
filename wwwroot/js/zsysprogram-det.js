// 2026-10-28 14:00 added: 系統程式資料維護 - detail page (Vue Composition API)
(function () {
    const { createApp, ref, onMounted } = Vue;

    const detHost = document.getElementById('program-det-app');
    const detToken = detHost.querySelector('input[name="__RequestVerificationToken"]').value;
    const parentId = detHost.dataset.parentId; // master FORM_ID from the url (?parentId=S01)

    createApp({
        setup() {
            const master = ref(null);        // the master row
            const detailList = ref([]);
            const detailLoading = ref(true);
            const error = ref('');

            // create / edit form
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

            // Master: one row
            async function loadMaster() {
                try {
                    const url = detHost.dataset.masterOneUrl + '?formId=' + encodeURIComponent(parentId);
                    master.value = await getJson(url);
                } catch (requestError) {
                    error.value = '找不到主選單 ' + parentId + '。';
                }
            }

            // Detail: children of the master
            async function loadDetail() {
                detailLoading.value = true;
                try {
                    const url = detHost.dataset.detailUrl + '?parentId=' + encodeURIComponent(parentId);
                    detailList.value = await getJson(url);
                } catch (requestError) {
                    error.value = requestError.message;
                } finally {
                    detailLoading.value = false;
                }
            }

            // show / hide the Bootstrap modal
            function showModal() {
                bootstrap.Modal.getOrCreateInstance(formModal.value).show();
            }

            function hideModal() {
                bootstrap.Modal.getOrCreateInstance(formModal.value).hide();
            }

            // Open an empty form. Parent = master FORM_ID (filled in automatically)
            function openCreate() {
                isNew.value = true;
                formTitle.value = '新增 Detail';
                formError.value = '';

                form.value = {
                    rowId: 0,
                    formId: '',
                    formCaption: '',
                    parentId: master.value.formId,
                    sortNo: '',
                    controllerName: '',
                    actionName: '',
                    routeName: '',
                    isEnabled: 1,
                    remark: ''
                };
                showModal();
            }

            // Open the form with one row
            function openEdit(item) {
                isNew.value = false;
                formTitle.value = '修改 Detail';
                formError.value = '';

                // Copy the row, so the table does not change before saving
                form.value = {
                    rowId: item.rowId,
                    formId: item.formId,
                    formCaption: item.formCaption,
                    parentId: master.value.formId,
                    sortNo: item.sortNo,
                    controllerName: item.controllerName,
                    actionName: item.actionName,
                    routeName: item.routeName,
                    isEnabled: item.isEnabled,
                    remark: item.remark
                };
                showModal();
            }

            // Save (create or edit)
            async function saveForm() {
                if (busy.value) {
                    return;
                }
                busy.value = true;
                formError.value = '';

                try {
                    const url = isNew.value ? detHost.dataset.createUrl : detHost.dataset.updateUrl;
                    await postJson(url, form.value);
                    hideModal();
                    await loadDetail();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            // Delete one detail row
            async function deleteRow(item) {
                if (!confirm('確定要刪除 ' + item.formId + ' ' + item.formCaption + ' ？')) {
                    return;
                }

                error.value = '';
                try {
                    const url = detHost.dataset.deleteUrl + '?rowId=' + item.rowId;
                    await postJson(url, {});
                    await loadDetail();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            onMounted(async function () {
                await loadMaster();
                await loadDetail();
            });

            return {
                master, detailList, detailLoading, error,
                formModal, isNew, formTitle, form, formError, busy,
                openCreate, openEdit, saveForm, deleteRow
            };
        }
    }).mount(detHost);
})();
