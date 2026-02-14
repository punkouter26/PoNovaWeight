# ApiContract.md - API Specs & Error Handling Policy

## PoNovaWeight API Specification

This document describes the REST API endpoints, request/response formats, and error handling policies.

---

## REGULAR VERSION

### Authentication Endpoints

#### GET /auth/login
Initiates Google OAuth flow.

**Response**: 302 Redirect to Google OAuth consent page

#### GET /auth/callback
Handles OAuth callback from Google.

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| code | string | Yes | Authorization code from Google |
| state | string | Yes | State token for CSRF protection |

**Response**: 302 Redirect to /auth/microsoft-callback

#### GET /auth/microsoft-callback
Finalizes authentication and creates session.

**Response**: 302 Redirect to Dashboard with auth cookie set

#### POST /auth/logout
Logs out the current user.

**Response**: 302 Redirect to Login page

#### GET /auth/status
Checks authentication status.

**Response**:
```json
{
  "isAuthenticated": true,
  "userId": "user@example.com",
  "displayName": "John Doe",
  "pictureUrl": "https://..."
}
```

---

### Daily Logs Endpoints

#### GET /api/dailylogs/{date}
Gets a specific day's log.

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| date | string | Date in yyyy-MM-dd format |

**Response** (200 OK):
```json
{
  "date": "2026-02-14",
  "proteins": 5,
  "vegetables": 3,
  "fruits": 2,
  "starches": 1,
  "fats": 2,
  "dairy": 0,
  "waterSegments": 4,
  "weight": 175.5,
  "omadCompliant": true,
  "alcoholConsumed": false
}
```

#### PUT /api/dailylogs
Creates or updates a daily log.

**Request Body**:
```json
{
  "date": "2026-02-14",
  "proteins": 5,
  "vegetables": 3,
  "fruits": 2,
  "starches": 1,
  "fats": 2,
  "dairy": 0,
  "waterSegments": 4,
  "weight": 175.5,
  "omadCompliant": true,
  "alcoholConsumed": false,
  "clientDate": "2026-02-14"
}
```

**Response** (200 OK): DailyLogDto

#### POST /api/dailylogs/increment
Increments a unit count for a specific category.

**Request Body**:
```json
{
  "date": "2026-02-14",
  "category": "Proteins",
  "clientDate": "2026-02-14"
}
```

**Response** (200 OK): DailyLogDto

#### PATCH /api/dailylogs/water
Updates water segment count.

**Request Body**:
```json
{
  "date": "2026-02-14",
  "increment": true,
  "clientDate": "2026-02-14"
}
```

**Response** (200 OK): DailyLogDto

#### DELETE /api/dailylogs/{date}
Deletes a daily log.

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| date | string | Date in yyyy-MM-dd format |

**Response**: 204 No Content

#### GET /api/dailylogs/{year}/{month}
Gets all logs for a specific month.

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| year | int | Year (e.g., 2026) |
| month | int | Month (1-12) |

**Response** (200 OK):
```json
{
  "year": 2026,
  "month": 2,
  "dailyLogs": [DailyLogDto]
}
```

#### GET /api/dailylogs/streak
Gets OMAD streak information.

**Response** (200 OK):
```json
{
  "currentStreak": 5,
  "longestStreak": 14,
  "lastOmadDate": "2026-02-14"
}
```

#### GET /api/dailylogs/trends
Gets weight trends for the last 30 days.

**Response** (200 OK):
```json
{
  "weights": [
    { "date": "2026-01-15", "weight": 178.0 },
    { "date": "2026-01-16", "weight": 177.5 }
  ],
  "averageWeight": 175.0,
  "minWeight": 172.0,
  "maxWeight": 180.0,
  "averageDailyChange": -0.2
}
```

#### GET /api/dailylogs/alcohol-correlation
Gets alcohol's impact on weight.

**Response** (200 OK):
```json
{
  "averageWeightWithAlcohol": 178.5,
  "averageWeightWithoutAlcohol": 174.0,
  "daysWithAlcohol": 4,
  "daysWithoutAlcohol": 26,
  "correlation": "positive"
}
```

---

### Weekly Summary Endpoints

#### GET /api/weeklysummary/{date}
Gets weekly summary starting from the Monday of the given date's week.

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| date | string | Date in yyyy-MM-dd format |

**Response** (200 OK):
```json
{
  "weekStartDate": "2026-02-09",
  "totalProteins": 35,
  "totalVegetables": 28,
  "totalFruits": 14,
  "totalStarches": 10,
  "totalFats": 12,
  "totalDairy": 5,
  "totalWaterSegments": 45,
  "averageWeight": 175.2,
  "omadDays": 6,
  "daysLogged": 7
}
```

---

### Meal Scan Endpoints

#### POST /api/mealscan
Analyzes a meal photo using AI.

**Request Body**:
```json
{
  "imageBase64": "data:image/jpeg;base64,...",
  "date": "2026-02-14"
}
```

**Response** (200 OK):
```json
{
  "suggestedUnits": {
    "proteins": 4,
    "vegetables": 3,
    "fruits": 0,
    "starches": 2,
    "fats": 1,
    "dairy": 0
  },
  "confidence": 0.85,
  "description": "Grilled chicken with mixed vegetables"
}
```

---

### Diagnostics Endpoints

#### GET /health
Liveness probe - checks if the application is running.

**Response**: 200 OK with health status

#### GET /alive
Readiness probe - checks if the application can serve traffic.

**Response**: 200 OK if ready

---

### Error Response Format

All error responses follow RFC 7807 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "The 'proteins' field must be between 0 and 100.",
  "errors": {
    "proteins": ["Must be between 0 and 100"]
  }
}
```

### HTTP Status Codes

| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful GET, PUT, PATCH |
| 201 | Created | Successful POST (resource created) |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Invalid request body or parameters |
| 401 | Unauthorized | Not authenticated |
| 403 | Forbidden | Authenticated but not authorized |
| 404 | Not Found | Resource not found |
| 422 | Unprocessable Entity | Validation failed |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service unavailable |

---

## SIMPLIFIED VERSION

### Main Endpoints

| Endpoint | Method | Description |
|---------|--------|-------------|
| /auth/login | GET | Start Google login |
| /auth/logout | POST | Log out |
| /auth/status | GET | Check if logged in |
| /api/dailylogs/{date} | GET | Get day's log |
| /api/dailylogs | PUT | Save day's log |
| /api/dailylogs/increment | POST | Add food unit |
| /api/dailylogs/water | PATCH | Update water |
| /api/dailylogs/{year}/{month} | GET | Get month |
| /api/dailylogs/streak | GET | Get OMAD streaks |
| /api/dailylogs/trends | GET | Get weight trends |
| /api/weeklysummary/{date} | GET | Get week summary |
| /api/mealscan | POST | AI scan meal |
| /health | GET | Health check |

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| 401 | Not logged in | Redirect to /auth/login |
| 400 | Invalid data | Check request body |
| 404 | Date not found | Create new log |
| 500 | Server error | Try again later |

### Example: Get Today's Log

```
GET /api/dailylogs/2026-02-14
Authorization: Bearer (session cookie)

Response:
{
  "date": "2026-02-14",
  "proteins": 5,
  "vegetables": 3,
  "waterSegments": 4,
  "weight": 175.5
}
```

### Example: Add Food

```
POST /api/dailylogs/increment
{
  "date": "2026-02-14",
  "category": "Proteins"
}

Response:
{
  "proteins": 6,
  ...
}
```
