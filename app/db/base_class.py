from sqlalchemy.orm import as_declarative, declared_attr

@as_declarative()
class Base:
    id: any
    __name__: str

    # to generate tablename from classname
    @declared_attr
    def __tablename__(cls) -> str:
        return cls.__name__.lower() 
