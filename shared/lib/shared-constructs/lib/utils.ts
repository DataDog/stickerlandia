/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { execSync } from "child_process";

/**
 * Get AWS account ID from environment or by calling STS.
 *
 * This is useful when CDK_DEFAULT_ACCOUNT is not set (e.g., with SSO credentials).
 * Falls back to calling `aws sts get-caller-identity` to resolve the account.
 *
 * @returns The AWS account ID, or undefined if it cannot be determined
 */
export function getAwsAccount(): string | undefined {
  if (process.env.CDK_DEFAULT_ACCOUNT) {
    return process.env.CDK_DEFAULT_ACCOUNT;
  }
  try {
    return execSync("aws sts get-caller-identity --query Account --output text", {
      encoding: "utf-8",
      stdio: ["pipe", "pipe", "pipe"],
    }).trim();
  } catch {
    return undefined;
  }
}
