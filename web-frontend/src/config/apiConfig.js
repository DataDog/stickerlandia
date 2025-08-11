class ApiConfig {
  constructor() {
    // Default to standalone mode, unless VITE_MODE=hotcontainer
    this.mode = import.meta.env.VITE_MODE || 'standalone'
  }

  get bffBaseUrl() {
    switch (this.mode) {
      case 'hotcontainer':
        return '/api/app/auth'
      case 'standalone':
      default:
        return 'http://localhost:8080/api/app/auth'
    }
  }

  get apiBaseUrl() {
    switch (this.mode) {
      case 'hotcontainer':
        return 'http://localhost:8080'
      case 'standalone':
      default:
        return 'http://localhost:8080'
    }
  }

  isStandaloneMode() {
    return this.mode === 'standalone'
  }

  isHotContainerMode() {
    return this.mode === 'hotcontainer'
  }
}

export default new ApiConfig()