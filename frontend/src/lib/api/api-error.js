export class ApiError extends Error {
  constructor(message, status, problem = null) {
    super(message);

    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}