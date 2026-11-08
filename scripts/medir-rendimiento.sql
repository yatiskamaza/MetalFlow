/*
    Medicion del requisito RNF-01 sobre el catalogo.

    Compara el comportamiento de la consulta parametrica con y sin el indice
    compuesto, de modo que la contribucion de este resulte cuantificable y no
    meramente afirmada.

    La ejecucion con el indice deshabilitado NO debe practicarse sobre datos
    reales: el indice se restituye al final del script, pero una interrupcion
    intermedia lo dejaria inoperativo.
*/

SET STATISTICS TIME ON;
SET STATISTICS IO ON;

PRINT '=== Consulta parametrica CON indice ===';

SELECT TOP 20 i.Id, i.LargoMm, i.AnchoMm, i.EspesorMm, i.Cantidad
FROM ITEM_INVENTARIO i
WHERE i.Estado = 1
  AND i.EspesorMm = 6
  AND i.LargoMm >= 1200 AND i.LargoMm <= 1500
  AND i.AnchoMm > 600
ORDER BY i.FechaAlta DESC;

PRINT '=== Deshabilitando el indice ===';
ALTER INDEX IX_ITEM_BUSQUEDA_PARAMETRICA ON ITEM_INVENTARIO DISABLE;

PRINT '=== Misma consulta SIN indice ===';

SELECT TOP 20 i.Id, i.LargoMm, i.AnchoMm, i.EspesorMm, i.Cantidad
FROM ITEM_INVENTARIO i
WHERE i.Estado = 1
  AND i.EspesorMm = 6
  AND i.LargoMm >= 1200 AND i.LargoMm <= 1500
  AND i.AnchoMm > 600
ORDER BY i.FechaAlta DESC;

PRINT '=== Restituyendo el indice ===';
ALTER INDEX IX_ITEM_BUSQUEDA_PARAMETRICA ON ITEM_INVENTARIO REBUILD;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO