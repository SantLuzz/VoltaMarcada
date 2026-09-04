const databaseName = "volta-marcada-work-order-photo-staging";
const databaseVersion = 1;
const storeName = "photos";

function getKey(workOrderId, photoType) {
    if (!workOrderId || photoType === null || photoType === undefined) {
        throw new Error("A work order and photo type are required.");
    }

    return `${workOrderId}:${photoType}`;
}

function openDatabase() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(databaseName, databaseVersion);

        request.onupgradeneeded = () => {
            const database = request.result;
            if (!database.objectStoreNames.contains(storeName)) {
                database.createObjectStore(storeName, { keyPath: "key" });
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error ?? new Error("Unable to open photo staging storage."));
    });
}

async function executeTransaction(mode, operation) {
    const database = await openDatabase();
    try {
        return await new Promise((resolve, reject) => {
            const transaction = database.transaction(storeName, mode);
            const store = transaction.objectStore(storeName);
            const request = operation(store);
            let result;

            request.onsuccess = () => {
                result = request.result;
            };
            request.onerror = () => reject(request.error ?? new Error("Unable to update photo staging storage."));
            transaction.oncomplete = () => resolve(result);
            transaction.onabort = () => reject(transaction.error ?? new Error("Photo staging transaction was aborted."));
        });
    } finally {
        database.close();
    }
}

export async function stagePhoto(workOrderId, photoType, fileName, contentType, contentBase64) {
    const key = getKey(workOrderId, photoType);
    if (!fileName || !contentType || !contentBase64) {
        throw new Error("Photo metadata and content are required.");
    }

    await executeTransaction("readwrite", store => store.put({
        key,
        workOrderId,
        photoType,
        fileName,
        contentType,
        contentBase64,
        stagedAtUtc: new Date().toISOString()
    }));
}

export async function restoreStagedPhoto(workOrderId, photoType) {
    return await executeTransaction("readonly", store => store.get(getKey(workOrderId, photoType)));
}

export async function clearStagedPhoto(workOrderId, photoType) {
    await executeTransaction("readwrite", store => store.delete(getKey(workOrderId, photoType)));
}

export async function initializePhotoInput(inputId, previewId, statusId, workOrderId, photoType) {
    const input = document.getElementById(inputId);
    if (!input || input.dataset.workOrderPhotoInitialized === "true") return;
    input.dataset.workOrderPhotoInitialized = "true";
    input.addEventListener("change", async () => {
        const file = input.files?.[0];
        if (!file || file.size > 10 * 1024 * 1024) { setStatus(statusId, "Selecione uma foto de até 10 MB."); return; }
        showPreview(previewId, file);
        setStatus(statusId, "Enviando foto...");
        await stageBrowserFile(workOrderId, photoType, file);
        await restoreAndUploadStagedPhoto(workOrderId, photoType, statusId);
    });
}
export async function restoreAndUploadStagedPhoto(workOrderId, photoType, statusId = null) {
    const staged = await restoreStagedPhoto(workOrderId, photoType);
    if (!staged?.file) return;
    const data = new FormData();
    data.append("file", staged.file, staged.fileName);
    data.append("type", String(photoType));
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) data.append("__RequestVerificationToken", token);
    const response = await fetch(`/work-orders/${workOrderId}/photos?type=${encodeURIComponent(photoType)}`, { method: "POST", body: data, credentials: "same-origin" });
    if (!response.ok) { setStatus(statusId, "Não foi possível salvar a foto. Tente novamente."); return; }
    await clearStagedPhoto(workOrderId, photoType);
    setStatus(statusId, "Foto enviada com sucesso.");
    window.dispatchEvent(new CustomEvent("volta-marcada:work-order-photo-uploaded", { detail: { workOrderId, photoType } }));
}

async function stageBrowserFile(workOrderId, photoType, file) {
    await executeTransaction("readwrite", store => store.put({
        key: getKey(workOrderId, photoType), workOrderId, photoType, fileName: file.name, contentType: file.type, file,
        stagedAtUtc: new Date().toISOString()
    }));
}

function setStatus(statusId, message) { const status = statusId ? document.getElementById(statusId) : null; if (status) status.textContent = message; }
function showPreview(previewId, file) { const preview = document.getElementById(previewId); if (!preview) return; const image = document.createElement('img'); image.className = 'work-order-photo-preview-pending'; image.alt = 'Prévia da foto selecionada'; image.src = URL.createObjectURL(file); preview.prepend(image); }
