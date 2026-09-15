$content = Get-Content -Raw (Join-Path $PSScriptRoot 'index.html')

if ($content -notmatch '<h3>Financeiro bÃ¡sico</h3>') {
    throw 'The landing must show the basic financial module.'
}

if ($content -notmatch 'recebimentos e o caixa') {
    throw 'The financial module must describe receipts and cash operations.'
}

if ($content -match 'pagamento online|automaÃ§Ã£o de WhatsApp|confirmaÃ§Ã£o automÃ¡tica') {
    throw 'The landing must not promise unsupported capabilities.'
}

