using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace VoltaMarcada.Web.Help;

public sealed record HelpGuide(string Id, string Title, string FileName);

public interface IHelpGuideProvider
{
    IReadOnlyList<HelpGuide> GetGuides();
    Task<MarkupString> GetContentAsync(string guideId, CancellationToken cancellationToken = default);
}

public sealed class HelpGuideProvider(IWebHostEnvironment environment) : IHelpGuideProvider
{
    private static readonly IReadOnlyList<HelpGuide> Guides =
    [
        new("cadastros-clientes", "Cadastros e clientes", "cadastros-clientes.md"),
        new("equipamentos", "Equipamentos", "equipamentos.md"),
        new("ordens-servico", "Ordens de serviço", "ordens-servico.md"),
        new("agenda", "Agenda", "agenda.md"),
        new("manutencao-recorrente", "Manutenção recorrente", "manutencao-recorrente.md"),
        new("orcamentos", "Orçamentos", "orcamentos.md"),
        new("solicitacoes-publicas", "Solicitações públicas", "solicitacoes-publicas.md")
    ];

    public IReadOnlyList<HelpGuide> GetGuides() => Guides;

    public async Task<MarkupString> GetContentAsync(string guideId, CancellationToken cancellationToken = default)
    {
        var guide = Guides.FirstOrDefault(item => item.Id == guideId) ?? Guides[0];
        var path = Path.Combine(environment.ContentRootPath, "HelpGuides", guide.FileName);
        var markdown = await File.ReadAllTextAsync(path, cancellationToken);
        return new MarkupString(RenderMarkdown(markdown));
    }

    private static string RenderMarkdown(string markdown)
    {
        var builder = new StringBuilder();
        var inList = false;
        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) { CloseList(builder, ref inList); continue; }
            if (line.StartsWith("# ")) { CloseList(builder, ref inList); builder.Append("<h2>").Append(FormatInline(line[2..])).Append("</h2>"); continue; }
            if (line.StartsWith("## ")) { CloseList(builder, ref inList); builder.Append("<h3>").Append(FormatInline(line[3..])).Append("</h3>"); continue; }
            if (Regex.IsMatch(line, "^\\d+\\. "))
            {
                if (!inList) { builder.Append("<ol>"); inList = true; }
                builder.Append("<li>").Append(FormatInline(Regex.Replace(line, "^\\d+\\. ", string.Empty))).Append("</li>");
                continue;
            }
            CloseList(builder, ref inList);
            builder.Append("<p>").Append(FormatInline(line)).Append("</p>");
        }
        CloseList(builder, ref inList);
        return builder.ToString();
    }

    private static void CloseList(StringBuilder builder, ref bool inList)
    {
        if (!inList) return;
        builder.Append("</ol>");
        inList = false;
    }

    private static string FormatInline(string text)
    {
        var encoded = WebUtility.HtmlEncode(text);
        encoded = Regex.Replace(encoded, "\\*\\*(.+?)\\*\\*", "<strong>$1</strong>");
        return Regex.Replace(encoded, "\\[([^]]+)\\]\\((/[^)]+)\\)", "<a class=\"help-guide-link\" href=\"$2\">$1</a>");
    }
}
