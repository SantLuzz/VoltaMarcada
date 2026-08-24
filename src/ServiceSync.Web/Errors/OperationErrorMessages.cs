namespace ServiceSync.Web.Errors;

public static class OperationErrorMessages
{
    private const string FallbackMessage = "Não foi possível concluir a operação. Tente novamente.";

    private static readonly IReadOnlyDictionary<string, string> Messages =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Customer.InvalidInput"] = "Revise os dados do cliente e tente novamente.",
            ["Customer.InvalidLocation"] = "Selecione um estado e uma cidade válidos.",
            ["Customer.DuplicateDocument"] = "Já existe um cliente com este CPF ou CNPJ.",
            ["Customer.NotFound"] = "Cliente não encontrado.",
            ["Customer.HasLinkedEquipment"] = "Não é possível excluir um cliente que possui equipamentos vinculados.",
            ["Equipment.InvalidInput"] = "Revise os dados do equipamento e tente novamente.",
            ["Equipment.CustomerNotFound"] = "Selecione um cliente válido da sua empresa.",
            ["Equipment.NotFound"] = "Equipamento não encontrado.",
            ["Service.InvalidInput"] = "Revise os dados do serviço e tente novamente.",
            ["Service.NotFound"] = "Serviço não encontrado.",
            ["Professional.InvalidInput"] = "Revise os dados do profissional e tente novamente.",
            ["Professional.NotFound"] = "Profissional não encontrado.",
            ["Professional.InvalidCompanyMember"] = "Selecione um funcionário disponível da sua empresa.",
            ["Professional.InvalidService"] = "Selecione serviços ativos válidos da sua empresa.",
            ["Professional.Forbidden"] = "Você não tem permissão para gerenciar profissionais.",
            ["ProfessionalAvailability.InvalidInput"] = "Revise os dados da disponibilidade e tente novamente.",
            ["ProfessionalAvailability.ProfessionalNotFound"] = "Profissional não encontrado.",
            ["ProfessionalAvailability.ScheduleBlockNotFound"] = "Bloqueio de agenda não encontrado.",
            ["ProfessionalAvailability.Forbidden"] = "Você não tem permissão para alterar esta disponibilidade.",
            ["CompanyOnboarding.Unauthenticated"] = "Entre novamente para concluir o cadastro da empresa.",
            ["CompanyOnboarding.AlreadyOnboarded"] = "Sua conta já está vinculada a uma empresa.",
            ["CompanyOnboarding.InvalidInput"] = "Revise os dados da empresa e tente novamente.",
            ["CompanyOnboarding.DocumentAlreadyExists"] = "Já existe uma empresa cadastrada com este CPF ou CNPJ.",
            ["CompanyOnboarding.SlugAlreadyExists"] = "Não foi possível gerar o endereço público da empresa. Tente novamente.",
            ["CompanyOnboarding.InvalidLocation"] = "Selecione um estado e uma cidade válidos.",
            ["CompanyOnboarding.InvalidLogo"] = "Envie uma imagem PNG, JPEG ou WebP de até 5 MB.",
            ["EmployeeInvitation.InvalidInput"] = "Revise os dados do funcionário e tente novamente.",
            ["EmployeeInvitation.Unauthenticated"] = "Entre novamente para continuar.",
            ["EmployeeInvitation.Unauthorized"] = "Você não tem permissão para gerenciar este convite.",
            ["EmployeeInvitation.EmailAlreadyRegistered"] = "Já existe uma conta cadastrada com este e-mail.",
            ["EmployeeInvitation.DuplicateEmail"] = "Já existe um convite pendente para este e-mail.",
            ["EmployeeInvitation.DuplicateDocument"] = "Já existe um funcionário ou convite com este CPF.",
            ["EmployeeInvitation.NotFound"] = "Convite não encontrado.",
            ["EmployeeInvitation.Unavailable"] = "Este convite não está mais disponível.",
            ["Scheduling.InvalidInput"] = "Revise os dados do agendamento.",
            ["Scheduling.NotFound"] = "Um dos dados do agendamento não está mais disponível.",
            ["Scheduling.Forbidden"] = "Você não tem permissão para realizar esta ação na agenda.",
            ["Scheduling.ProfessionalUnavailable"] = "O profissional não está disponível nesse horário.",
            ["Scheduling.ProfessionalNotQualified"] = "O profissional não está habilitado para este serviço.",
            ["Scheduling.AppointmentConflict"] = "O horário acabou de ser ocupado. Escolha outro horário.",
            ["Scheduling.InvalidStatus"] = "O agendamento não permite essa ação no estado atual.",
            ["Scheduling.BlockRequestRequiresAppointmentResolution"] = "Existem atendimentos que precisam ser ajustados antes da aprovação.",
            ["WorkOrder.InvalidInput"] = "Revise os dados da ordem de serviço.",
            ["WorkOrder.NotFound"] = "Ordem de serviço não encontrada.",
            ["WorkOrder.Forbidden"] = "Você não tem permissão para realizar esta ação na ordem de serviço.",
            ["WorkOrder.CustomerNotFound"] = "Cliente não encontrado.",
            ["WorkOrder.EquipmentNotFound"] = "Equipamento não encontrado.",
            ["WorkOrder.InvalidEquipmentCustomer"] = "O equipamento não pertence ao cliente selecionado.",
            ["WorkOrder.ServiceNotFound"] = "Serviço não encontrado.",
            ["WorkOrder.ProfessionalNotFound"] = "Profissional não encontrado.",
            ["WorkOrder.ProfessionalNotQualified"] = "O profissional não está habilitado para o serviço.",
            ["WorkOrder.ProfessionalProfileRequired"] = "É necessário possuir um perfil profissional ativo.",
            ["WorkOrder.ProfessionalRequired"] = "Selecione um profissional antes de agendar o atendimento.",
            ["WorkOrder.ProfessionalUnavailable"] = "O profissional não está disponível no horário selecionado.",
            ["WorkOrder.InvalidStatus"] = "A ordem de serviço não permite esta ação no estado atual.",
            ["WorkOrder.ChecklistIncomplete"] = "Conclua os itens obrigatórios do checklist.",
            ["WorkOrder.BeforePhotoRequired"] = "Inclua ao menos uma foto antes.",
            ["WorkOrder.AfterPhotoRequired"] = "Inclua ao menos uma foto depois.",
            ["WorkOrder.OpenAppointments"] = "Conclua ou cancele os agendamentos abertos antes de concluir a ordem de serviço.",
            ["WorkOrder.PhotoLimitReached"] = "O limite de fotos da ordem de serviço foi atingido.",
            ["WorkOrder.InvalidPhoto"] = "Envie uma imagem JPEG, PNG, WebP ou HEIC de até 10 MB."
        };

    public static string GetMessage(string? code) =>
        code is not null && Messages.TryGetValue(code, out var message)
            ? message
            : FallbackMessage;
}
