// Nova Food Journal - Budget Module
// Creates consumption budget with cost alerts

@description('Budget name')
param name string

@description('Budget amount in USD')
@minValue(1)
@maxValue(1000)
param amount int = 5

@description('Email address for budget alerts')
param alertEmail string

@description('Budget start date (YYYY-MM-DD format, first of month)')
param startDate string

// Budget
resource budget 'Microsoft.Consumption/budgets@2023-11-01' = {
  name: name
  properties: {
    category: 'Cost'
    amount: amount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: startDate
    }
    notifications: {
      // Alert at 50% of budget
      Notification50Percent: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 50
        contactEmails: [
          alertEmail
        ]
        thresholdType: 'Actual'
      }
      // Alert at 80% of budget (constitution requirement)
      Notification80Percent: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 80
        contactEmails: [
          alertEmail
        ]
        thresholdType: 'Actual'
      }
      // Alert at 100% of budget
      Notification100Percent: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100
        contactEmails: [
          alertEmail
        ]
        thresholdType: 'Actual'
      }
      // Forecasted alert at 100%
      NotificationForecasted: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100
        contactEmails: [
          alertEmail
        ]
        thresholdType: 'Forecasted'
      }
    }
  }
}

// Outputs
output id string = budget.id
output name string = budget.name
