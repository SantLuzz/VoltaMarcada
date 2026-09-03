namespace VoltaMarcada.Application.WorkOrders;

public static class WorkOrderHistoryDescriptions
{
    public static string Created() => "OS criada.";
    public static string DetailsUpdated() => "Dados da OS atualizados.";
    public static string ProfessionalAssigned() => "Profissional responsável atualizado.";
    public static string Started() => "Atendimento iniciado.";
    public static string ChecklistProgressSaved() => "Progresso do checklist atualizado.";
    public static string ExecutionNotesUpdated() => "Observações da execução atualizadas.";
    public static string PhotoAdded() => "Foto adicionada.";
    public static string PhotoRemoved() => "Foto removida.";
    public static string AttachmentAdded(string fileName) => $"Anexo adicionado: {NormalizeFileName(fileName)}.";
    public static string AttachmentRemoved(string fileName) => $"Anexo removido: {NormalizeFileName(fileName)}.";
    public static string AdditionalItemsUpdated() => "Itens adicionais atualizados.";
    public static string Completed() => "Atendimento concluído.";
    public static string Cancelled() => "OS cancelada.";

    private static string NormalizeFileName(string fileName) => string.IsNullOrWhiteSpace(fileName) ? "arquivo" : fileName.Trim()[..Math.Min(fileName.Trim().Length, 120)];
}
