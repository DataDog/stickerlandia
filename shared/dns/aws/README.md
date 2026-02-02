# Shared DNS

This shared infrastructure code contains the required IaC for provisioning DNS resources inside AWS (Route53, certificates etc).

If you are deploying resources to test in your own environment, this is not required. The DNS details are only loaded in CI environments (`dev`, `prod`).