Con el fin de corroborar correctamente el paso de los datos de las DB's entre ambientes
se crearon los dos scipts contenidos en esta carpeta, para hacerlo correctamente debes
seguir los siguientes pasos.

PASOS:

1. Debes ejecutar el script "CreateProcedureCompareTables.sql" en cualquiera de tus DB locales
2. Al momento de ejecutar el comando "EXEC CompareTables 'DB1', 'DB2', 'table_name', 'name_column'

	DB1->Es la db origin de la cual vas a obtener los datos princiaples 
	DB2->Es la db contra la cual vas a comparar
	table_name-> Nombre de la tabla que se encuentra en ambas DB por la cual vas a comparar
	column_name-> Nombre de la columna de la tabla por la cual vas a comparar sus valores

Una vez ejecutado el procedimiento "CompareTales" correctamente el resultado esperado sería
los datos de la DB1 que no se encuentran en la tabla especificada en la DB2