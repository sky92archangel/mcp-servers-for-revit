import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateModelCurveTool(server: McpServer) {
  server.tool(
    "create_model_curve",
    "Create model curves in the Revit model. Supports lines and arcs with start/end points, curve type, and sketch plane level. All units in mm.\n在 Revit 中创建模型线。支持直线和弧线，可设置起点/终点、曲线类型和草图平面标高。所有单位为毫米。",
    {
      data: z.array(z.object({
        startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point in mm / 起点坐标（毫米）"),
        endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point in mm / 终点坐标（毫米）"),
        curveType: z.string().optional().describe("Curve type: Line, Arc / 曲线类型"),
        sketchPlaneLevel: z.number().optional().describe("Sketch plane level elevation in mm / 草图平面标高（毫米）"),
      })).describe("Array of model curves to create / 要创建的模型线数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_model_curve", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create model curve failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
