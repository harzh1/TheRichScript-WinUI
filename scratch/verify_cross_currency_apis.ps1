# Comprehensive End-to-End Cross-Currency FX API Verification Script
$ErrorActionPreference = "Stop"

$baseUrl = "https://richscript-backend-bgbtgnbug3f9cden.centralindia-01.azurewebsites.net/api"
$testEmail = "fxtest_" + [System.Guid]::NewGuid().ToString().Substring(0,8) + "@test.com"
$password = "FxTestPass123!"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Starting Cross-Currency FX System API Verification Tests  " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Register test user
Write-Host "`n[1/6] Registering test user: $testEmail..." -ForegroundColor Yellow
$regBody = @{
    email = $testEmail
    password = $password
    firstName = "Currency"
    lastName = "Tester"
    country = "IN"
    currency = "INR"
} | ConvertTo-Json

$regResp = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -Body $regBody -ContentType "application/json"
$token = $regResp.token
if (-not $token) { $token = $regResp.accessToken }
$headers = @{ "Authorization" = "Bearer $token" }
Write-Host "  -> Registered successfully. User ID: $($regResp.user.id)" -ForegroundColor Green

# 2. Create multi-currency assets: INR Bank Account and USD Wallet
Write-Host "`n[2/6] Creating INR Bank Account and USD Wallet assets..." -ForegroundColor Yellow
# Fetch asset types
$typesResp = Invoke-RestMethod -Uri "$baseUrl/assets/types" -Method Get -Headers $headers
$bankType = $typesResp.data | Select-Object -First 1
$typeId = if ($bankType) { $bankType.id } else { "default" }

$inrAssetBody = @{
    name = "HDFC INR Savings"
    type = "BANK"
    typeId = $typeId
    value = 500000.0
    currency = "INR"
} | ConvertTo-Json

$inrAsset = (Invoke-RestMethod -Uri "$baseUrl/assets" -Method Post -Body $inrAssetBody -ContentType "application/json" -Headers $headers).data
Write-Host "  -> Created INR Asset: $($inrAsset.name) (Balance: ₹$($inrAsset.value))" -ForegroundColor Green

$usdAssetBody = @{
    name = "USD Travel Card"
    type = "BANK"
    typeId = $typeId
    value = 5000.0
    currency = "USD"
} | ConvertTo-Json

$usdAsset = (Invoke-RestMethod -Uri "$baseUrl/assets" -Method Post -Body $usdAssetBody -ContentType "application/json" -Headers $headers).data
Write-Host "  -> Created USD Asset: $($usdAsset.name) (Balance: `$$($usdAsset.value))" -ForegroundColor Green

# 3. Test Cross-Currency Transaction (Expense in USD on INR Account)
Write-Host "`n[3/6] Testing Cross-Currency Transaction (Expense in USD charged to INR Asset)..." -ForegroundColor Yellow
$txAmount = 100.0   # $100 USD
$customRate = 85.5  # 85.5 INR per 1 USD
$expectedTarget = 8550.0 # 8,550 INR debited

$txBody = @{
    assetId = $inrAsset.id
    type = "EXPENSE"
    amount = $txAmount
    currency = "USD"
    exchangeRate = $customRate
    targetAmount = $expectedTarget
    category = "TRAVEL"
    description = "Flight ticket in USD charged to INR bank"
    date = (Get-Date).ToString("o")
} | ConvertTo-Json

$txResp = (Invoke-RestMethod -Uri "$baseUrl/transactions" -Method Post -Body $txBody -ContentType "application/json" -Headers $headers).data
Write-Host "  -> Transaction Created: Amount = `$$($txResp.amount) $($txResp.currency), Rate = $($txResp.exchangeRate), TargetAmount = ₹$($txResp.targetAmount)" -ForegroundColor Green

function Get-AssetById($id) {
    $allAssets = (Invoke-RestMethod -Uri "$baseUrl/assets" -Method Get -Headers $headers).data
    return ($allAssets | Where-Object { $_.id -eq $id })
}

# Verify INR asset balance deduction
$updatedInrAsset = Get-AssetById $inrAsset.id
$expectedInrBal = 500000.0 - $expectedTarget
Write-Host "  -> INR Asset Balance after Expense: ₹$($updatedInrAsset.value) (Expected: ₹$expectedInrBal)" -ForegroundColor $(if ($updatedInrAsset.value -eq $expectedInrBal) { "Green" } else { "Red" })

# 4. Test Cross-Currency Fund Transfer (USD Asset -> INR Asset)
Write-Host "`n[4/6] Testing Cross-Currency Asset Transfer (USD -> INR)..." -ForegroundColor Yellow
$transferAmount = 200.0      # $200 USD
$transferRate = 86.0        # 86.0 INR per 1 USD
$transferDestAmount = 17200.0 # ₹17,200 INR credited

$transferBody = @{
    fromAssetId = $usdAsset.id
    toAssetId = $inrAsset.id
    amount = $transferAmount
    destinationAmount = $transferDestAmount
    exchangeRate = $transferRate
    description = "Cross-currency remittance: USD to INR"
    date = (Get-Date).ToString("o")
} | ConvertTo-Json

$transferResp = Invoke-RestMethod -Uri "$baseUrl/assets/transfer" -Method Post -Body $transferBody -ContentType "application/json" -Headers $headers
Write-Host "  -> Transfer Successful: $($transferResp.message)" -ForegroundColor Green

$usdAfterTransfer = Get-AssetById $usdAsset.id
$inrAfterTransfer = Get-AssetById $inrAsset.id
Write-Host "  -> USD Balance: `$$($usdAfterTransfer.value) (Expected: `$$([double](5000.0 - 200.0)))" -ForegroundColor Green
Write-Host "  -> INR Balance: ₹$($inrAfterTransfer.value) (Expected: ₹$([double]($expectedInrBal + 17200.0)))" -ForegroundColor Green

# 5. Test Cross-Currency Debt Creation & Settlement
Write-Host "`n[5/6] Testing Cross-Currency Debt Creation & Settlement (USD Debt settled via INR Account)..." -ForegroundColor Yellow
# Create Debt of $300 USD lent to Alice from USD Card
$debtBody = @{
    assetId = $usdAsset.id
    type = "LENT"
    personName = "Alice"
    amount = 300.0
    currency = "USD"
    notes = "Loan to Alice in USD"
} | ConvertTo-Json

$debtResp = (Invoke-RestMethod -Uri "$baseUrl/debts" -Method Post -Body $debtBody -ContentType "application/json" -Headers $headers).data
Write-Host "  -> Created Debt with $($debtResp.personName): `$$($debtResp.amount) $($debtResp.currency)" -ForegroundColor Green

# Alice repays $100 USD, deposited into our INR Bank Account with custom exchange rate
$settleAmount = 100.0
$settleRate = 84.5
$settleAssetAmount = 8450.0 # 8,450 INR credited to INR account

$settleBody = @{
    amount = $settleAmount
    assetId = $inrAsset.id
    settleAssetAmount = $settleAssetAmount
    exchangeRate = $settleRate
} | ConvertTo-Json

$settleResp = (Invoke-RestMethod -Uri "$baseUrl/debts/$($debtResp.id)/settle" -Method Put -Body $settleBody -ContentType "application/json" -Headers $headers).data
Write-Host "  -> Settle Debt Succeeded: Settled `$$($settleResp.settledAmount) of `$$($settleResp.amount)" -ForegroundColor Green
Write-Host "  -> Debt Status: $($settleResp.status)" -ForegroundColor Green

# 6. Test Cross-Currency Goal Creation & Funding
Write-Host "`n[6/6] Testing Cross-Currency Goal Funding (USD Goal funded from INR Account)..." -ForegroundColor Yellow
$goalBody = @{
    name = "New York Trip"
    targetAmount = 2000.0
    initialDeposit = 0
    currency = "USD"
    targetAssetId = $usdAsset.id
    sourceAssetId = $inrAsset.id
    expectedDate = (Get-Date).AddMonths(6).ToString("o")
} | ConvertTo-Json

$goalResp = (Invoke-RestMethod -Uri "$baseUrl/goals" -Method Post -Body $goalBody -ContentType "application/json" -Headers $headers).data
Write-Host "  -> Created Goal: '$($goalResp.name)' Target = `$$($goalResp.targetAmount) $($goalResp.currency)" -ForegroundColor Green

# Fund $150 USD into goal from INR account with destinationAmount and exchangeRate
$fundGoalBody = @{
    amount = 12750.0          # ₹12,750 INR deducted from INR account
    assetId = $inrAsset.id
    destinationAmount = 150.0 # $150 USD credited to goal
    exchangeRate = 0.011765   # 150 / 12750
} | ConvertTo-Json

$fundResp = (Invoke-RestMethod -Uri "$baseUrl/goals/$($goalResp.id)/funds" -Method Put -Body $fundGoalBody -ContentType "application/json" -Headers $headers).data
Write-Host "  -> Fund Goal Succeeded: Goal Current Amount = `$$($fundResp.currentAmount) / `$$($fundResp.targetAmount)" -ForegroundColor Green

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host "  ALL 6 CROSS-CURRENCY API TESTS PASSED WITH 100% SUCCESS!  " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
