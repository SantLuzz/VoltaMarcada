const databaseName = "volta-marcada-work-order-photo-staging";
const databaseVersion = 1;
const storeName = "photos";
const supportedContentTypes = new Set(["image/jpeg", "image/png", "image/webp", "image/heic"]);
const maximumPhotoSizeBytes = 10 * 1024 * 1024;

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

export async function discardStagedPhoto(workOrderId, photoType, inputId = null, previewId = null, statusId = null) {
    await clearStagedPhoto(workOrderId, photoType);
    const input = inputId ? document.getElementById(inputId) : null;
    if (input) input.value = "";
    const preview = previewId ? document.getElementById(previewId) : null;
    if (preview) preview.replaceChildren();
    setStatus(statusId, "");
}

export async function initializePhotoInput(inputId, previewId, statusId, workOrderId, photoType) {
    const input = document.getElementById(inputId);
    if (!input || input.dataset.workOrderPhotoInitialized === "true") return;
    input.dataset.workOrderPhotoInitialized = "true";
    input.addEventListener("change", async () => {
        const file = input.files?.[0];
        if (!file) return;
        if (!supportedContentTypes.has(file.type) || file.size > maximumPhotoSizeBytes) {
            input.value = "";
            setStatus(statusId, "Selecione uma foto JPEG, PNG, WebP ou HEIC de até 10 MB.");
            showValidationToast("Selecione uma foto JPEG, PNG, WebP ou HEIC de até 10 MB.");
            return;
        }

        showPreview(previewId, file, async () => {
            await discardStagedPhoto(workOrderId, photoType, inputId, previewId, statusId);
        });
        setStatus(statusId, "Enviando foto...");
        try {
            await stageBrowserFile(workOrderId, photoType, file);
            await restoreAndUploadStagedPhoto(workOrderId, photoType, statusId);
        } catch {
            await discardStagedPhoto(workOrderId, photoType, inputId, previewId);
            setStatus(statusId, "Não foi possível salvar a foto. Tente novamente.");
        }
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
    try {
        const response = await fetch(`/work-orders/${workOrderId}/photos?type=${encodeURIComponent(photoType)}`, { method: "POST", body: data, credentials: "same-origin" });
        if (!response.ok) { await clearStagedPhoto(workOrderId, photoType); setStatus(statusId, "Não foi possível salvar a foto. Tente novamente."); return; }
        await clearStagedPhoto(workOrderId, photoType);
        setStatus(statusId, "Foto enviada com sucesso.");
        window.dispatchEvent(new CustomEvent("volta-marcada:work-order-photo-uploaded", { detail: { workOrderId, photoType } }));
    } catch {
        await clearStagedPhoto(workOrderId, photoType);
        setStatus(statusId, "Não foi possível salvar a foto. Tente novamente.");
    }
}
async function stageBrowserFile(workOrderId, photoType, file) {
    await executeTransaction("readwrite", store => store.put({
        key: getKey(workOrderId, photoType), workOrderId, photoType, fileName: file.name, contentType: file.type, file,
        stagedAtUtc: new Date().toISOString()
    }));
}

function setStatus(statusId, message) { const status = statusId ? document.getElementById(statusId) : null; if (status) status.textContent = message; }
function showValidationToast(message) {
    const existingToast = document.getElementById("work-order-photo-validation-toast");
    if (existingToast) existingToast.remove();

    const toast = document.createElement("div");
    toast.id = "work-order-photo-validation-toast";
    toast.setAttribute("role", "alert");
    toast.textContent = message;
    Object.assign(toast.style, {
        position: "fixed", right: "24px", bottom: "24px", zIndex: "1400",
        maxWidth: "360px", padding: "12px 16px", borderRadius: "4px",
        background: "#d32f2f", color: "#ffffff", boxShadow: "0 3px 5px rgba(0,0,0,.2)"
    });
    document.body.append(toast);
    window.setTimeout(() => toast.remove(), 8000);
}

function showPreview(previewId, file, onDiscard) {
    const preview = document.getElementById(previewId);
    if (!preview) return;

    preview.replaceChildren();
    const container = document.createElement("div");
    container.className = "work-order-photo-card work-order-photo-preview-pending";
    const image = document.createElement("img");
    image.alt = "Prévia da foto selecionada";
    image.src = URL.createObjectURL(file);
    const discardButton = document.createElement("button");
    discardButton.type = "button";
    discardButton.className = "work-order-photo-discard-button";
    discardButton.setAttribute("aria-label", "Remover foto pendente");
    discardButton.textContent = "×";
    discardButton.addEventListener("click", onDiscard);
    container.append(image, discardButton);
    preview.prepend(container);
}