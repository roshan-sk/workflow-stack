**SAMPLE ERP APPLICATION**

This application is built to test a Workflow Service.It is not a full production application, but a simplified version created for testing workflow integration.

**About the Application**
**ERP Service Flow**

1. An employee (user) registers using username, email, and password.
2. The user is redirected to the login page and enters credentials.
3. After login, the user is redirected to the request page to submit a purchase request.
4. The ERP system saves the request in the database and calls the middleware indicating a new request is created.
5. The middleware starts the workflow process.
6. The workflow triggers a notify manager step, calling the ERP API to send an email.
7. The ERP sends an email to the manager using the mail service.
8. The manager receives an email with request details and approval/rejection links.
9. When the manager clicks a link, it redirects to the corresponding API.
10. The API processes the request and sends the decision to the middleware.
11. The middleware signals the workflow to continue the next step.
12. This process continues through Finance and HR until completion.

Here we used JWT token, when user login, a jwt token is generated and we make remaining APIs as authorized apis, that means only user with token only access apis

Here I implemented JWT using a private and public key pair. The private key is used to generate the token, and the public key is used to validate and read it.

## Service Token

Some internal APIs (like notification APIs) use a service token. This is a pre-generated static token shared between services for secure communication.

APIs list:

1. /register-page - register html page to enter details in frontend
2. /register - register api, this will save details
3. /login-page - login htm page, to enter login details in frontend
4. /login - login api, this used to login user
5. /request – request HTML page to submit a new purchase request details in frontend.
6. /start – API to save a new purchase request and calls Middleware. Requires JWT authorization.
7. /manager/approve/{id} – Manager Approve request API.
8. /manager/reject/{id} – Manager Reject request API.
9. /finance/approve/{id} – Finance Approve request API.
10. /finance/reject/{id} – Finance Reject request API.
11. /hr/approve/{id} – HR Approve request API.
12. /hr/reject/{id} – HR Reject request API.
13. /api/purchase-requests/{id}/notify-manager – Notify manager via email with approve/reject links. Uses service token.
14. /api/purchase-requests/{id}/notify-finance – Notify finance via email with approve/reject links. Uses service token.
15. /api/purchase-requests/{id}/notify-hr – Notify HR via email with approve/reject links. Uses service token.
16. /api/purchase-requests/{id}/notify-rejection – Notify requestor via email that the request was rejected. Uses service token.
17. /api/purchase-requests/{id}/close – Mark request as completed and notify requestor via email. Uses service token.
18. /action – Redirects to login page with request details

## Overall Flow Summary

User registers → /register-page → /register.
User logs in → /login-page → /login → JWT token returned.
User submits request → /request → /start.
Managers, Finance, HR approve/reject → /manager/-, /finance/-, /hr/-.
Notifications sent via /api/purchase-requests/Id/notify-\*.

## Step to Generating RSA Keys(Private - Public keys)

You can generate RSA keys using OpenSSL
Step 1: Check opnssl is installed or not if not install - Windows: Use https://slproweb.com/products/Win32OpenSSL.html
Step 2: Create a folder for keys
'''mkdir keys
'''cd keys
Step 3: Generate Private Key
'''openssl genpkey -algorithm RSA -out private_key.pem -pkeyopt rsa_keygen_bits:2048
This creates a 2048-bit RSA private key.
File: keys/private_key.pem
Step 4: Generate Public Key
'''openssl rsa -pubout -in private_key.pem -out public_key.pem
This extracts the public key from the private key.
File: keys/public_key.pem
Step 5: Save private_key.pem and public_key.pem in a folder called keys at your project root.

## Setup

1. Clone the repository.
2. Create `.env` file with following variables:

```text
EMAIL_HOST=
EMAIL_PORT=
EMAIL_USER=
EMAIL_PASS=
LOCALHOST_URL=
NOTIFY_TO=
SERVICE_TOKEN=
```
