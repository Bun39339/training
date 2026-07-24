$in = [Console]::In.ReadToEnd()

try {
    $payload = $in | ConvertFrom-Json
    $toolName = if ($payload.tool_name) { $payload.tool_name } else { '<missing>' }
    $toolInputType = if ($null -eq $payload.tool_input) {
        '<missing>'
    }
    else {
        $payload.tool_input.GetType().Name
    }
    $toolInputKeys = if ($payload.tool_input -is [PSCustomObject]) {
        ($payload.tool_input.PSObject.Properties.Name -join ',')
    }
    else {
        '<none>'
    }
    $blockedPattern = 'DROP TABLE|' + 'TRUN' + 'CATE'
    $rawMatch = [bool]($in -match $blockedPattern)
    $commandMatch = [bool](
        $payload.tool_input -is [PSCustomObject] -and
        $payload.tool_input.command -match $blockedPattern
    )
    $timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffK'
    Add-Content -LiteralPath '.codex/hooks/tool-name.log' -Value (
        "$timestamp`ttool_name=$toolName`ttool_input_type=$toolInputType" +
        "`ttool_input_keys=$toolInputKeys`traw_match=$rawMatch`tcommand_match=$commandMatch"
    )
}
catch {
    $timestamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffK'
    Add-Content -LiteralPath '.codex/hooks/tool-name.log' -Value "$timestamp`t<invalid-json>"
}

if ($in -match 'DROP TABLE|TRUNCATE') {
    [Console]::Error.WriteLine('Action denied')
    exit 2
}
exit 0
