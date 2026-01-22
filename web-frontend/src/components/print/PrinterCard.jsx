/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import LocalPrintshopOutlinedIcon from '@mui/icons-material/LocalPrintshopOutlined'

export default function PrinterCard({ printer, onPrintClick }) {
  return (
    <div className="landing-card">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <LocalPrintshopOutlinedIcon className="text-gray-600" />
          <h3 className="font-bold text-xl">{printer.printerName}</h3>
        </div>

        {/* Status indicator: color + dot + text (accessible) */}
        <div className={`flex items-center gap-1.5 px-2 py-1 rounded-full text-xs font-medium ${
          printer.isOnline
            ? 'bg-green-100 text-green-800'
            : 'bg-gray-100 text-gray-600'
        }`}>
          <span
            className={`w-2 h-2 rounded-full ${printer.isOnline ? 'bg-green-500' : 'bg-gray-400'}`}
            aria-hidden="true"
          />
          {printer.isOnline ? 'Online' : 'Offline'}
        </div>
      </div>

      {printer.lastJobProcessed && (
        <p className="text-sm text-gray-500 mb-4">
          Last job: {new Date(printer.lastJobProcessed).toLocaleString()}
        </p>
      )}

      <button
        onClick={onPrintClick}
        disabled={!printer.isOnline}
        title={!printer.isOnline ? 'This printer is currently offline' : 'Print to this printer'}
        className={`w-full py-2 px-4 rounded-lg font-medium transition-colors ${
          printer.isOnline
            ? 'bg-purple-600 text-white hover:bg-purple-700'
            : 'bg-gray-200 text-gray-400 cursor-not-allowed'
        }`}
      >
        {printer.isOnline ? 'Print' : 'Offline'}
      </button>
    </div>
  )
}
