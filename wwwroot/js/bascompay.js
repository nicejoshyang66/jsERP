// 2026-11-09 14:30 added: 客戶 / 供應商基本資料 (Vue Composition API)
// Same page for both; data-class-id is 'A' (客戶) or 'B' (供應商)
(function () {
    const { createApp, ref, computed, onMounted } = Vue; // 2026-11-09 15:46 modified: add computed

    const host = document.getElementById('compay-app');
    const token = host.querySelector('input[name="__RequestVerificationToken"]').value;
    const urls = host.dataset; // all data-xxx of the page
    const classId = host.dataset.classId;

    createApp({
        setup() {
            const rows = ref([]);
            const keyword = ref('');
            const loading = ref(true);
            const error = ref('');
            const className = host.dataset.className;

            // Modal and its form
            const formModal = ref(null);
            const form = ref({});
            const isNew = ref(true);        // true = create, false = edit
            const formError = ref('');
            const busy = ref(false);
            const isView = ref(false);      // 2026-11-09 15:46 added: true = view only (瀏覽)

            // ---------- Paging (front-end) ---------- 2026-11-09 15:46 added
            const page = ref(1);            // current page
            const pageSize = ref(10);       // rows per page
            const pageSizeOptions = [5, 10, 20, 50];

            // Total rows
            const totalCount = computed(function () {
                return rows.value.length;
            });

            // Total pages
            const totalPages = computed(function () {
                return Math.ceil(totalCount.value / pageSize.value);
            });

            // First row number of this page (1-based)
            const startItem = computed(function () {
                if (totalCount.value === 0) {
                    return 0;
                }
                return (page.value - 1) * pageSize.value + 1;
            });

            // Last row number of this page
            const endItem = computed(function () {
                let end = page.value * pageSize.value;
                if (end > totalCount.value) {
                    end = totalCount.value;
                }
                return end;
            });

            // Rows shown on this page
            const pagedRows = computed(function () {
                const start = (page.value - 1) * pageSize.value;
                return rows.value.slice(start, start + pageSize.value);
            });

            // Page numbers: current page and 2 pages before / after
            const pageNumbers = computed(function () {
                let startPage = page.value - 2;
                let endPage = page.value + 2;
                if (startPage < 1) {
                    startPage = 1;
                }
                if (endPage > totalPages.value) {
                    endPage = totalPages.value;
                }
                const list = [];
                for (let i = startPage; i <= endPage; i++) {
                    list.push(i);
                }
                return list;
            });

            // Go to page n
            function goPage(n) {
                if (n < 1 || n > totalPages.value) {
                    return;
                }
                page.value = n;
            }

            // Rows per page changed: back to page 1
            function changePageSize() {
                page.value = 1;
            }

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

            function showModal() {
                bootstrap.Modal.getOrCreateInstance(formModal.value).show();
            }

            function hideModal() {
                bootstrap.Modal.getOrCreateInstance(formModal.value).hide();
            }

            // ---------- Query ----------

            async function loadRows() {
                error.value = '';
                loading.value = true;
                try {
                    const url = urls.rowsUrl
                        + '?classId=' + encodeURIComponent(classId)
                        + '&keyword=' + encodeURIComponent(keyword.value);
                    rows.value = await getJson(url);
                    // 2026-11-09 15:46 added: after delete, the page may be past the last page
                    if (page.value > totalPages.value) {
                        page.value = totalPages.value > 0 ? totalPages.value : 1;
                    }
                } catch (requestError) {
                    error.value = requestError.message;
                } finally {
                    loading.value = false;
                }
            }

            // 2026-11-09 15:46 added: search button, start from page 1
            async function searchRows() {
                page.value = 1;
                await loadRows();
            }

            // ---------- Create / Edit / View ----------

            function openCreate() {
                isNew.value = true;
                isView.value = false; // 2026-11-09 15:46 added
                formError.value = '';
                form.value = {
                    debitId: '', debitName: '', debitNick: '', taxId: '',
                    careerPic: '', contact: '', address: '', mailingAddress: '',
                    billingAddress: '', tel: '', fax: '', email: '', remark: ''
                };
                showModal();
            }

            function openEdit(item) {
                isNew.value = false;
                isView.value = false; // 2026-11-09 15:46 added
                formError.value = '';
                // Copy the row, so the table does not change before saving
                form.value = Object.assign({}, item);
                showModal();
            }

            // 2026-11-09 15:46 added: view one row, fields are read only
            function openView(item) {
                isNew.value = false;
                isView.value = true;
                formError.value = '';
                form.value = Object.assign({}, item);
                showModal();
            }

            async function saveRow() {
                if (busy.value || isView.value) { // 2026-11-09 15:46 modified: no save in view mode
                    return;
                }
                busy.value = true;
                formError.value = '';
                try {
                    let url = urls.updateUrl;
                    if (isNew.value) {
                        url = urls.createUrl + '?classId=' + encodeURIComponent(classId);
                    }
                    const result = await postJson(url, form.value);
                    hideModal();
                    if (isNew.value && result.message && result.message !== '新增成功。') {
                        alert(result.message); // company was the other class, only the class is added
                    }
                    await loadRows();
                } catch (requestError) {
                    formError.value = requestError.message;
                } finally {
                    busy.value = false;
                }
            }

            // ---------- Delete ----------

            async function deleteRow(item) {
                const text = '確定要刪除 ' + item.debitId + ' ' + (item.debitNick || '') + ' ？\n'
                    + '(客戶及供應商資料都會一起刪除)';
                if (!confirm(text)) {
                    return;
                }
                error.value = '';
                try {
                    await postJson(urls.deleteUrl + '?debitId=' + encodeURIComponent(item.debitId), {});
                    await loadRows();
                } catch (requestError) {
                    error.value = requestError.message;
                }
            }

            onMounted(loadRows);

            return {
                rows, keyword, loading, error, className,
                formModal, form, isNew, formError, busy,
                loadRows, openCreate, openEdit, saveRow, deleteRow,
                // 2026-11-09 15:46 added: view + paging
                isView, openView, searchRows,
                page, pageSize, pageSizeOptions, totalCount, totalPages,
                startItem, endItem, pagedRows, pageNumbers, goPage, changePageSize
            };
        }
    }).mount('#compay-app');
})();
