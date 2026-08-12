# Speed-test clarification: PCIe, TCP and RDMA with Qwen3.6-27B

[Русская версия](README.md)

This is a **clarifying supplement** to the [RTX 5090 + Tesla V100 RPC over 25 GbE benchmark](../rpc-25gbe/README.en.md). The original article measured the regular RPC path over TCP/IP. This follow-up isolates one Qwen3.6-27B profile and compares three transport modes: local PCIe, inter-machine TCP and inter-machine RDMA.

> This does not replace the original article and is not a universal interface ranking. It answers a narrower question: how much RDMA reduced transport overhead relative to TCP in this configuration, and whether a gap to local PCIe remained.

## What was compared

| Mode | Topology | Role |
|---|---|---|
| PCIe | Two GPUs in one computer | Local baseline |
| TCP | Two computers, direct 25 GbE link | Regular inter-machine transport |
| RDMA | Two computers, the same 25 GbE link | Inter-machine transport with lower network-stack overhead |

All six runs used `Qwen3.6-27B-UD-Q8_K_XL`, F16 and MTP 5 with the same 17,112 input tokens and 1,536 generated tokens. Each mode was measured twice; the table reports the mean.

## Results

| Mode | Prompt, tok/s | Generation, tok/s | Draft acceptance | Mean draft length |
|---|---:|---:|---:|---:|
| PCIe | **1749.50** | **76.07** | **74.6%** | **4.73** |
| TCP | 1033.60 | 58.11 | 72.9% | 4.64 |
| RDMA | 1471.26 | 63.70 | 69.4% | 4.47 |

![PCIe, TCP and RDMA comparison](assets/transport-comparison.png)

### RDMA versus TCP

- Prompt processing: `1471.26` versus `1033.60` tok/s, making RDMA about **42.3%** faster.
- Generation: `63.70` versus `58.11` tok/s, an RDMA advantage of about **9.6%**.
- The prompt-processing gap to local PCIe shrank from **40.9%** with TCP to **15.9%** with RDMA.
- The generation gap to PCIe shrank from **23.6%** to **16.3%**.

The largest RDMA effect appears during long-prompt processing: it recovered almost two thirds of the performance lost by TCP relative to PCIe. The generation gain is smaller because autoregressive decoding depends more on serial computation and latency, not transport bandwidth alone.

## Why PCIe remained faster

RDMA reduces network-stack overhead, but it does not turn two computers into one local system. Cross-node latency, RPC synchronization and intermediate-data transfers remain. RDMA therefore moved the result substantially closer to PCIe without reaching local speed in these measurements.

Draft acceptance was also lower with RDMA than with TCP and PCIe. The entire difference should therefore not be attributed automatically to transport alone: MTP efficiency varied between runs. A stricter isolation of the network effect would require a longer series, network telemetry and a separate test without speculative decoding.

## Two-run spread

| Mode | Prompt: minimum–maximum | Generation: minimum–maximum |
|---|---:|---:|
| PCIe | 1498.76–2000.24 | 74.69–77.44 |
| TCP | 912.99–1154.20 | 57.83–58.39 |
| RDMA | 1460.41–1482.11 | 62.99–64.40 |

Two runs are enough for a practical clarification, but not for a statistically robust conclusion. Prompt processing varies notably for PCIe and TCP, so the percentages describe this dataset rather than a guaranteed acceleration factor.

![Raw measurements for all modes](assets/raw-measurements.png)

## Practical takeaway

For this model and 25 GbE link, RDMA helped most during large-context prefill. It substantially narrowed, but did not eliminate, the gap to local PCIe. RDMA looks considerably more attractive than regular TCP when prompt-processing speed matters. If generation speed is the only concern, the gain is real but much smaller.

## Data and limitations

- [Excel workbook with raw measurements, formulas and charts](data/qwen_pcie_tcp_rdma.xlsx)
- The dataset contains two runs per mode.
- It does not record the llama.cpp version, RDMA configuration, network utilization, latency, remote CPU/RAM or complete command line.
- The `RDMA` label describes the mode recorded in the measurement log; the dataset alone does not prove GPUDirect RDMA or direct GPU access to remote-node memory.

