# Kubernetes deployment

The manifests intentionally do not contain secrets. Create the namespace and one Secret before applying the services:

```powershell
kubectl apply -f .\deploy\kubernetes\namespace.yaml

$jwtBytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
$jwtKey = [Convert]::ToBase64String($jwtBytes)

kubectl -n ecommerce create secret generic ecommerce-secrets `
  --from-literal=jwt-signing-key="$jwtKey" `
  --from-literal=user-connection="Server=sql-server;Database=ECommerceUserDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True" `
  --from-literal=product-connection="Server=sql-server;Database=ECommerceProductDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True" `
  --from-literal=order-connection="Server=sql-server;Database=ECommerceOrderDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True" `
  --from-literal=payment-connection="Server=sql-server;Database=ECommercePaymentDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True"

kubectl apply -k .\deploy\kubernetes
kubectl -n ecommerce get deployments,services,pods
```

Replace `CHANGE_ME` and the SQL hostname with values for your cluster. If the GHCR packages are private, create an image-pull secret and add it to each pod specification. Database migrations should be run as a controlled deployment job before rolling out a new application image.
