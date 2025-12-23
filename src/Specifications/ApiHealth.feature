# language: es
@api @smoke

Característica: Salud de la API
  Como equipo de QA
  Quiero validar un endpoint público sencillo
  Para garantizar que la capa de API responde

  @api @smoke
  Escenario: GET saludable retorna 200
    Cuando realizo un GET a "https://httpbin.org/get"
    Entonces el codigo de respuesta debe ser 200
    Y la respuesta tiene la propiedad "url"
